using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Common.Lib.Contracts;

namespace Common.Providers.Queue;

/// <summary>Production queue provider (adrs/deployment/queue-based-decoupling.md).
/// Also serves the integration tests against LocalStack — the SDK client's
/// ServiceURL is the only thing that changes.</summary>
public sealed class SqsQueueProvider(IAmazonSQS sqs) : IQueueProvider
{
    public const string CorrelationAttribute = "correlationid";

    public async Task EnqueueAsync<TMessage>(
        string queueName, TMessage message, string correlationId, CancellationToken cancellationToken)
    {
        var queueUrl = (await sqs.GetQueueUrlAsync(queueName, cancellationToken)).QueueUrl;
        await sqs.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = queueUrl,
            MessageBody = JsonSerializer.Serialize(message),
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                [CorrelationAttribute] = new() { DataType = "String", StringValue = correlationId },
            },
        }, cancellationToken);
    }
}
