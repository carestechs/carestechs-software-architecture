using System.Text.Json;
using Auth.PreTokenGeneration;
using Common.Lib.Tenancy;
using Tenancy.Application.Contracts;
using Tenancy.Data;
using Xunit;

namespace Platform.Tests;

/// <summary>The pre-token trigger handler against a recorded event fixture and
/// the real DynamoDB directory (adrs/api/cognito-authentication.md — the
/// Cognito wiring itself is phase-3; the enrichment logic is provable now).</summary>
public class PreTokenHandlerTests
{
    private const string Fixture = """
        {
          "UserName": "ada",
          "ClientId": "test-client",
          "UserAttributes": { "custom:oid": "acme", "email": "ada@example.com" }
        }
        """;

    [Fact]
    public async Task EnrichesTenantClaims_FromTheGlobalDirectory()
    {
        Assert.SkipWhen(Environment.GetEnvironmentVariable("DDB_ENDPOINT_URL") is null,
            "DDB_ENDPOINT_URL not set — integration runs in CI against DynamoDB Local.");

        var ct = TestContext.Current.CancellationToken;
        using var dynamo = Messaging.Tests.TestEnv.Dynamo();
        await Messaging.Tests.TestEnv.EnsureTableAsync(
            dynamo, DynamoOrganizationDirectory.TableName, "orgid", null, ct);

        var directory = new DynamoOrganizationDirectory(dynamo);
        await directory.PutAsync(new OrganizationRecord("acme", "Acme Corp", "main", Enabled: true), ct);

        var handler = new PreTokenHandler(directory);
        var triggerEvent = JsonSerializer.Deserialize<PreTokenEvent>(Fixture)!;

        var claims = await handler.HandleAsync(triggerEvent, ct);
        Assert.NotNull(claims);
        Assert.Equal("acme", claims.ClaimsToAddOrOverride["org"]);
        Assert.Equal("main", claims.ClaimsToAddOrOverride["workspace"]);
        Assert.Equal("ada", claims.ClaimsToAddOrOverride["sub"]);
    }

    [Fact]
    public async Task DisabledOrganization_GetsNoClaims()
    {
        Assert.SkipWhen(Environment.GetEnvironmentVariable("DDB_ENDPOINT_URL") is null,
            "DDB_ENDPOINT_URL not set — integration runs in CI against DynamoDB Local.");

        var ct = TestContext.Current.CancellationToken;
        using var dynamo = Messaging.Tests.TestEnv.Dynamo();
        await Messaging.Tests.TestEnv.EnsureTableAsync(
            dynamo, DynamoOrganizationDirectory.TableName, "orgid", null, ct);

        var directory = new DynamoOrganizationDirectory(dynamo);
        await directory.PutAsync(new OrganizationRecord("dormant", "Dormant Inc", "main", Enabled: false), ct);

        var handler = new PreTokenHandler(directory);
        var claims = await handler.HandleAsync(new PreTokenEvent(
            "bob", "test-client", new() { ["custom:oid"] = "dormant" }), ct);

        // no claims -> deny-by-default at the APIs finishes the job
        Assert.Null(claims);
    }
}
