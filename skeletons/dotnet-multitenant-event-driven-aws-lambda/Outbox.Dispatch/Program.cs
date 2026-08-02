using Amazon.DynamoDBv2;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.SQS;
using Common.Lib.Tenancy;
using Microsoft.Extensions.Logging;
using Outbox.Dispatch;
using Tenancy.Data;

// Scheduled Lambda (EventBridge rate rule in the SAM template): iterate enabled
// organizations from the global directory and drain each tenant's outbox.
using var loggerFactory = LoggerFactory.Create(logging => logging.AddJsonConsole());
var logger = loggerFactory.CreateLogger<OutboxDispatcher>();

var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
var dbPort = int.Parse(Environment.GetEnvironmentVariable("DB_PORT") ?? "5432");
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "postgres";

using var sqs = new AmazonSQSClient();
using var dynamo = new AmazonDynamoDBClient();
var dispatcher = new OutboxDispatcher(sqs, dbHost, dbPort, dbUser, dbPassword, logger);
var directory = new DynamoOrganizationDirectory(dynamo);

var handler = async (string organizationId, ILambdaContext context) =>
{
    if (!OrgId.TryParse(organizationId, out var org))
    {
        return 0;
    }
    var organization = await directory.GetAsync(org, CancellationToken.None);
    if (organization is null || !organization.Enabled
        || !WorkspaceId.TryParse(organization.DefaultWorkspaceId, out var workspace))
    {
        return 0;
    }
    return await dispatcher.DrainTenantAsync(org, workspace, CancellationToken.None);
};

await LambdaBootstrapBuilder.Create(handler, new DefaultLambdaJsonSerializer())
    .Build()
    .RunAsync();
