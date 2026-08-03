namespace Orders.Data;

/// <summary>Outbox row (adrs/database/transactional-outbox.md). Data-layer type:
/// the outbox is persistence plumbing, not a domain concept.</summary>
public class OutboxMessage
{
    public Guid Id { get; set; }
    public string QueueName { get; set; } = "";
    public string Payload { get; set; } = "";
    public string CorrelationId { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? DispatchedAt { get; set; }
}
