using Common.Lib.Tenancy;
using Messaging.Data;
using Tenancy.Data;
using Xunit;

namespace Messaging.Tests;

/// <summary>The flagship claim, executed: two tenants provisioned from code
/// (CREATE DATABASE + full DbUp replay, schema-per-module inside), and data in
/// tenant A is structurally invisible to tenant B
/// (adrs/database/database-per-tenant.md, adrs/database/schema-per-module.md,
/// adrs/deployment/dbup-migrations.md).</summary>
public class TenantIsolationTests
{
    [Fact]
    public async Task ProvisionedTenants_AreStructurallyIsolated()
    {
        Assert.SkipWhen(TestEnv.PgAdmin is null || TestEnv.DdbEndpoint is null,
            "TEST_DATABASE_URL / DDB_ENDPOINT_URL not set — integration runs in CI.");

        var ct = TestContext.Current.CancellationToken;
        var (host, port, user, password) = TestEnv.PgParts();

        Assert.True(OrgId.TryParse("acme", out var acme));
        Assert.True(OrgId.TryParse("globex", out var globex));
        Assert.True(WorkspaceId.TryParse("main", out var main));

        var provisioner = new TenantDatabaseProvisioner(host, port, user, password);
        await provisioner.ProvisionAsync(acme, main, ct);   // full DbUp history replay
        await provisioner.ProvisionAsync(globex, main, ct); // identical schema for the new tenant

        using var dynamo = TestEnv.Dynamo();
        await TestEnv.EnsureTableAsync(dynamo, DynamoMessageStore.TableName, "pk", "sk", ct);

        var messaging = MessagingModule.Create(host, port, user, password, dynamo);

        var conversationId = await messaging.StartConversationAsync(
            "acme", "main", "Ada Lovelace", ct);

        var inAcme = await messaging.GetConversationAsync("acme", "main", conversationId, ct);
        Assert.NotNull(inAcme);
        Assert.Equal("Ada Lovelace", inAcme.ContactName);

        // the same id addressed through tenant B's scope hits a DIFFERENT database
        var inGlobex = await messaging.GetConversationAsync("globex", "main", conversationId, ct);
        Assert.Null(inGlobex);
    }

    [Fact]
    public async Task HotPathMessages_AreTenantScopedByPartitionKey()
    {
        Assert.SkipWhen(TestEnv.PgAdmin is null || TestEnv.DdbEndpoint is null,
            "TEST_DATABASE_URL / DDB_ENDPOINT_URL not set — integration runs in CI.");

        var ct = TestContext.Current.CancellationToken;
        var (host, port, user, password) = TestEnv.PgParts();
        Assert.True(OrgId.TryParse("acme", out var acme));
        Assert.True(WorkspaceId.TryParse("main", out var main));
        await new TenantDatabaseProvisioner(host, port, user, password).ProvisionAsync(acme, main, ct);

        using var dynamo = TestEnv.Dynamo();
        await TestEnv.EnsureTableAsync(dynamo, DynamoMessageStore.TableName, "pk", "sk", ct);
        var messaging = MessagingModule.Create(host, port, user, password, dynamo);

        var conversationId = await messaging.StartConversationAsync("acme", "main", "Grace Hopper", ct);
        Assert.True(await messaging.AppendMessageAsync(
            "acme", "main", conversationId, "grace", "Hello", "corr-a", ct));

        var messages = await messaging.ListMessagesAsync("acme", "main", conversationId, ct);
        var message = Assert.Single(messages);
        Assert.Equal("Hello", message.Text);

        // the same conversation id under another tenant's partition key is empty
        var foreign = await messaging.ListMessagesAsync("globex", "main", conversationId, ct);
        Assert.Empty(foreign);
    }
}
