using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using System.Globalization;
using Messaging.Application.Internal;

namespace Messaging.Data;

/// <summary>Hot-path message storage (adrs/database/dynamodb-hot-path.md):
/// tenant-scoped partition key, Query by key only — never a Scan.</summary>
internal sealed class DynamoMessageStore(IAmazonDynamoDB dynamo) : IMessageStore
{
    public const string TableName = "Message";

    private static string PartitionKey(string orgId, string workspaceId, Guid conversationId) =>
        $"{orgId}#{workspaceId}#{conversationId}";

    public async Task PutAsync(string orgId, string workspaceId, Guid conversationId,
        Guid messageId, string sender, string text, DateTimeOffset sentAt,
        CancellationToken cancellationToken)
    {
        await dynamo.PutItemAsync(new PutItemRequest
        {
            TableName = TableName,
            Item = new Dictionary<string, AttributeValue>
            {
                ["pk"] = new(PartitionKey(orgId, workspaceId, conversationId)),
                ["sk"] = new($"{sentAt.UtcTicks:D19}#{messageId}"),
                ["messageid"] = new(messageId.ToString()),
                ["sender"] = new(sender),
                ["text"] = new(text),
                ["sentat"] = new(sentAt.ToString("O", CultureInfo.InvariantCulture)),
            },
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<(Guid Id, string Sender, string Text, DateTimeOffset SentAt)>> ListAsync(
        string orgId, string workspaceId, Guid conversationId, CancellationToken cancellationToken)
    {
        var response = await dynamo.QueryAsync(new QueryRequest
        {
            TableName = TableName,
            KeyConditionExpression = "pk = :pk",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new(PartitionKey(orgId, workspaceId, conversationId)),
            },
        }, cancellationToken);

        return (response.Items ?? [])
            .Select(item => (
                Guid.Parse(item["messageid"].S),
                item["sender"].S,
                item["text"].S,
                DateTimeOffset.Parse(item["sentat"].S, CultureInfo.InvariantCulture)))
            .ToList();
    }
}
