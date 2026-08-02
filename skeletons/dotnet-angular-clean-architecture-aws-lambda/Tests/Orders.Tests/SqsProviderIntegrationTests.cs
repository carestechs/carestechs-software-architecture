using System.Text.Json;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Common.Core.Messages;
using Common.Providers.Queue;
using Xunit;

namespace Orders.Tests;

/// <summary>Round-trips the production SqsQueueProvider against LocalStack:
/// real SQS semantics, and the correlation attribute must survive the hop
/// (adrs/deployment/correlation-propagation.md).</summary>
public class SqsProviderIntegrationTests
{
    private static readonly string? EndpointUrl =
        Environment.GetEnvironmentVariable("AWS_ENDPOINT_URL");

    [Fact]
    public async Task Enqueue_RoundTripsPayloadAndCorrelationAttribute()
    {
        Assert.SkipWhen(EndpointUrl is null,
            "AWS_ENDPOINT_URL not set — SQS integration runs in CI against LocalStack.");

        var ct = TestContext.Current.CancellationToken;
        using var sqs = new AmazonSQSClient(
            new BasicAWSCredentials("test", "test"),
            new AmazonSQSConfig { ServiceURL = EndpointUrl, AuthenticationRegion = "us-east-1" });

        var queueName = $"it-{Guid.CreateVersion7():N}";
        var queueUrl = (await sqs.CreateQueueAsync(queueName, ct)).QueueUrl;
        try
        {
            var provider = new SqsQueueProvider(sqs);
            var message = new OrderPlacedMessage(Guid.CreateVersion7(), Guid.CreateVersion7(), 5);
            await provider.EnqueueAsync(queueName, message, "corr-xyz", ct);

            var received = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = queueUrl,
                MessageAttributeNames = ["All"],
                WaitTimeSeconds = 5,
            }, ct);

            var record = Assert.Single(received.Messages);
            Assert.Equal(message, JsonSerializer.Deserialize<OrderPlacedMessage>(record.Body));
            Assert.Equal("corr-xyz",
                record.MessageAttributes[SqsQueueProvider.CorrelationAttribute].StringValue);
        }
        finally
        {
            await sqs.DeleteQueueAsync(queueUrl, ct);
        }
    }
}
