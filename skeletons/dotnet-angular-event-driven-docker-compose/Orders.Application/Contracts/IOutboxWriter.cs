namespace Orders.Application.Contracts;

/// <summary>Records a message in the transactional outbox
/// (adrs/database/transactional-outbox.md). The row participates in the
/// caller's unit of work — nothing is published here.</summary>
public interface IOutboxWriter
{
    Task WriteAsync<TMessage>(
        string queueName, TMessage message, string correlationId, CancellationToken cancellationToken);
}
