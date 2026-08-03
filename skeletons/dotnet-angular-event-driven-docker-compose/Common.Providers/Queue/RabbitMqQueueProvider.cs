using System.Text.Json;
using Common.Lib.Contracts;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace Common.Providers.Queue;

/// <summary>Production queue provider (adrs/deployment/rabbitmq-broker.md) — and
/// the development one: the laptop runs the same broker. Publishes are durable
/// (persistent messages to durable queues) and declare their topology on the way,
/// so a publisher never races the consumer's declarations.</summary>
public sealed class RabbitMqQueueProvider(
    RabbitMqConnectionProvider connectionProvider,
    IConfiguration configuration) : IQueueProvider
{
    public async Task EnqueueAsync<TMessage>(
        string queueName, TMessage message, string correlationId, CancellationToken cancellationToken)
    {
        var connection = await connectionProvider.GetConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        var retryTtl = int.TryParse(configuration["RABBITMQ_RETRY_TTL_MS"], out var ttl) ? ttl : 5000;
        await RabbitMqTopology.DeclareWorkQueueAsync(channel, queueName, retryTtl, cancellationToken);

        await channel.BasicPublishAsync(
            RabbitMqTopology.WorkExchange,
            routingKey: queueName,
            mandatory: false,
            basicProperties: new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                Headers = new Dictionary<string, object?> { [RabbitMqHeaders.Correlation] = correlationId },
            },
            body: JsonSerializer.SerializeToUtf8Bytes(message),
            cancellationToken: cancellationToken);
    }
}
