using Amazon.DynamoDBv2;
using Messaging.Application;

namespace Messaging.Data;

/// <summary>The module's public composition entry point — hosts cold-start
/// through this, never through internal types (adrs/dotnet/module-facade.md).</summary>
public static class MessagingModule
{
    public static IMessagingModuleApi Create(
        string dbHost, int dbPort, string dbUser, string dbPassword, IAmazonDynamoDB dynamo) =>
        new MessagingModuleApi(
            new MessagingUnitOfWorkFactory(dbHost, dbPort, dbUser, dbPassword),
            new DynamoMessageStore(dynamo));
}
