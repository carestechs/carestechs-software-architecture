using RabbitMQ.Client;

namespace Common.Providers.Queue;

/// <summary>Topology as code (adrs/deployment/rabbitmq-broker.md): exchanges,
/// queues, bindings, and the retry/dead-letter wiring are declared idempotently
/// at startup and on publish — never hand-created in the management UI.
///
/// The retry policy IS the topology: a nack (requeue: false) on the work queue
/// dead-letters to the retry queue, whose per-queue TTL expires the message back
/// into the work queue. Poison messages are parked explicitly in the DLQ once
/// the attempt budget is spent.</summary>
public static class RabbitMqTopology
{
    public const string WorkExchange = "app.work";
    public const string RetryExchange = "app.retry";
    public const string DeadLetterExchange = "app.dlq";
    public const string RetrySuffix = ".retry";
    public const string DeadLetterSuffix = ".dlq";

    public static async Task DeclareWorkQueueAsync(
        IChannel channel, string queueName, int retryTtlMs, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(WorkExchange, ExchangeType.Direct,
            durable: true, autoDelete: false, cancellationToken: cancellationToken);
        await channel.ExchangeDeclareAsync(RetryExchange, ExchangeType.Direct,
            durable: true, autoDelete: false, cancellationToken: cancellationToken);
        await channel.ExchangeDeclareAsync(DeadLetterExchange, ExchangeType.Direct,
            durable: true, autoDelete: false, cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false,
            autoDelete: false, arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = RetryExchange,
                ["x-dead-letter-routing-key"] = queueName,
            }, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(queueName, WorkExchange, routingKey: queueName,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(queueName + RetrySuffix, durable: true, exclusive: false,
            autoDelete: false, arguments: new Dictionary<string, object?>
            {
                ["x-message-ttl"] = retryTtlMs,
                ["x-dead-letter-exchange"] = WorkExchange,
                ["x-dead-letter-routing-key"] = queueName,
            }, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(queueName + RetrySuffix, RetryExchange, routingKey: queueName,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(queueName + DeadLetterSuffix, durable: true,
            exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(queueName + DeadLetterSuffix, DeadLetterExchange,
            routingKey: queueName, cancellationToken: cancellationToken);
    }
}
