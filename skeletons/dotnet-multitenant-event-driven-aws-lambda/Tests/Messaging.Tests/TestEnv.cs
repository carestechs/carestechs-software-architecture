using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Npgsql;

namespace Messaging.Tests;

/// <summary>Shared environment plumbing for the env-gated integration tests.
/// CI provides PostgreSQL (admin connection), DynamoDB Local, and ElasticMQ.</summary>
public static class TestEnv
{
    public static string? PgAdmin => Environment.GetEnvironmentVariable("TEST_DATABASE_URL");
    public static string? DdbEndpoint => Environment.GetEnvironmentVariable("DDB_ENDPOINT_URL");
    public static string? SqsEndpoint => Environment.GetEnvironmentVariable("AWS_ENDPOINT_URL");

    public static (string Host, int Port, string User, string Password) PgParts()
    {
        var builder = new NpgsqlConnectionStringBuilder(PgAdmin);
        return (builder.Host!, builder.Port, builder.Username!, builder.Password!);
    }

    public static AmazonDynamoDBClient Dynamo() => new(
        new BasicAWSCredentials("test", "test"),
        new AmazonDynamoDBConfig { ServiceURL = DdbEndpoint, AuthenticationRegion = "us-east-1" });

    public static async Task EnsureTableAsync(
        IAmazonDynamoDB dynamo, string table, string hashKey,
        string? rangeKey, CancellationToken cancellationToken)
    {
        var existing = await dynamo.ListTablesAsync(cancellationToken);
        if (existing.TableNames?.Contains(table) == true)
        {
            return;
        }

        var schema = new List<KeySchemaElement> { new(hashKey, KeyType.HASH) };
        var attributes = new List<AttributeDefinition> { new(hashKey, ScalarAttributeType.S) };
        if (rangeKey is not null)
        {
            schema.Add(new KeySchemaElement(rangeKey, KeyType.RANGE));
            attributes.Add(new AttributeDefinition(rangeKey, ScalarAttributeType.S));
        }

        try
        {
            await dynamo.CreateTableAsync(new CreateTableRequest
            {
                TableName = table,
                KeySchema = schema,
                AttributeDefinitions = attributes,
                BillingMode = BillingMode.PAY_PER_REQUEST,
            }, cancellationToken);
        }
        catch (ResourceInUseException)
        {
            // test classes run in parallel — another one won the create race
        }
    }
}
