using System.Net.Http.Json;
using Common.Lib.Contracts;
using System.Text.Json;

namespace Common.Providers.Queue;

/// <summary>Dev queue provider: same enqueueing code, local HTTP queue server
/// instead of SQS (adrs/deployment/queue-based-decoupling.md).</summary>
public sealed class HttpQueueProvider(HttpClient httpClient) : IQueueProvider
{
    public async Task EnqueueAsync<TMessage>(
        string queueName, TMessage message, string correlationId, CancellationToken cancellationToken)
    {
        var envelope = new QueueEnvelope(correlationId, JsonSerializer.Serialize(message));
        var response = await httpClient.PostAsJsonAsync(
            $"/queues/{queueName}/messages", envelope, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
