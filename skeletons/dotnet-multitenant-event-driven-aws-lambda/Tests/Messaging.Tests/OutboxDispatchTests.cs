using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Common.Lib.Tenancy;
using Common.Providers.Queue;
using Messaging.Application;
using Messaging.Data;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Outbox.Dispatch;
using Tenancy.Data;
using Xunit;

namespace Messaging.Tests;

/// <summary>End to end (adrs/database/transactional-outbox.md): append writes
/// the outbox row in the tenant transaction; the dispatcher drains it to SQS
/// with the persisted correlation id and marks it dispatched — never deleted.</summary>
public class OutboxDispatchTests
{
    [Fact]
    public async Task AppendedMessage_RidesTheOutboxToSqs_AndIsMarkedDispatched()
    {
        Assert.SkipWhen(
            TestEnv.PgAdmin is null || TestEnv.DdbEndpoint is null || TestEnv.SqsEndpoint is null,
            "TEST_DATABASE_URL / DDB_ENDPOINT_URL / AWS_ENDPOINT_URL not set — integration runs in CI.");

        var ct = TestContext.Current.CancellationToken;
        var (host, port, user, password) = TestEnv.PgParts();
        Assert.True(OrgId.TryParse("outboxorg", out var org));
        Assert.True(WorkspaceId.TryParse("main", out var workspace));
        await new TenantDatabaseProvisioner(host, port, user, password).ProvisionAsync(org, workspace, ct);

        using var dynamo = TestEnv.Dynamo();
        await TestEnv.EnsureTableAsync(dynamo, DynamoMessageStore.TableName, "pk", "sk", ct);
        var messaging = MessagingModule.Create(host, port, user, password, dynamo);

        var conversationId = await messaging.StartConversationAsync("outboxorg", "main", "Alan Turing", ct);
        Assert.True(await messaging.AppendMessageAsync(
            "outboxorg", "main", conversationId, "alan", "Computable?", "corr-outbox", ct));

        using var sqs = new AmazonSQSClient(
            new BasicAWSCredentials("test", "test"),
            new AmazonSQSConfig { ServiceURL = TestEnv.SqsEndpoint, AuthenticationRegion = "us-east-1" });
        var queueUrl = (await sqs.CreateQueueAsync(MessagingModuleApi.MessageAppendedQueue, ct)).QueueUrl;

        var dispatcher = new OutboxDispatcher(
            sqs, host, port, user, password, NullLogger<OutboxDispatcher>.Instance);
        var dispatched = await dispatcher.DrainTenantAsync(org, workspace, ct);
        Assert.True(dispatched >= 1);

        var received = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = queueUrl,
            MessageAttributeNames = ["All"],
            WaitTimeSeconds = 5,
        }, ct);
        var record = Assert.Single(received.Messages);
        Assert.Contains(conversationId.ToString(), record.Body);
        Assert.Equal("corr-outbox",
            record.MessageAttributes[SqsQueueProvider.CorrelationAttribute].StringValue);

        // marked, not deleted — the outbox doubles as the audit trail
        var tenantConnection = TenantDbConnectionBuilder.BuildConnectionString(
            host, port, user, password, org, workspace);
        await using var connection = new NpgsqlConnection(tenantConnection);
        await connection.OpenAsync(ct);
        await using var count = new NpgsqlCommand(
            "SELECT count(*) FROM messaging.outbox WHERE dispatchedat IS NULL", connection);
        Assert.Equal(0L, await count.ExecuteScalarAsync(ct));
    }
}
