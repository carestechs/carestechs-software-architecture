using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Common.Providers.Queue;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Orders.Worker;

/// <summary>Production path: SQS-triggered Lambda with partial-batch failure
/// reporting — one poison record never recycles its batch
/// (adrs/deployment/idempotent-queue-consumers.md).</summary>
public class Function
{
    private readonly ServiceProvider _services = WorkerServices.Build(
        new ConfigurationBuilder().AddEnvironmentVariables().Build());

    public async Task<SQSBatchResponse> HandleAsync(SQSEvent sqsEvent, ILambdaContext context)
    {
        var failures = new List<SQSBatchResponse.BatchItemFailure>();
        foreach (var record in sqsEvent.Records)
        {
            using var scope = _services.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<OrderPlacedProcessor>();
            var correlation = record.MessageAttributes.TryGetValue(
                SqsQueueProvider.CorrelationAttribute, out var attribute)
                    ? attribute.StringValue
                    : null;
            var done = await processor.ProcessAsync(record.Body, correlation, CancellationToken.None);
            if (!done)
            {
                failures.Add(new SQSBatchResponse.BatchItemFailure { ItemIdentifier = record.MessageId });
            }
        }

        return new SQSBatchResponse(failures);
    }
}
