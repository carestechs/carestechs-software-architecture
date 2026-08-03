namespace Common.Lib.Contracts;

/// <summary>Cross-module async work rides queues (adrs/deployment/queue-based-decoupling.md).
/// The correlation id is REQUIRED on every enqueue (adrs/deployment/correlation-propagation.md).
/// The abstraction is the invariant; RabbitMQ is the broker behind it in every
/// environment (adrs/deployment/rabbitmq-broker.md).</summary>
public interface IQueueProvider
{
    Task EnqueueAsync<TMessage>(
        string queueName, TMessage message, string correlationId, CancellationToken cancellationToken);
}
