using System.Text.Json;
using Orders.Application.Contracts;

namespace Orders.Data;

/// <summary>Adds the row to the SAME OrdersDbContext the command handler saves —
/// that shared scoped context is what makes order + message atomic.</summary>
public sealed class OutboxWriter(OrdersDbContext context) : IOutboxWriter
{
    public Task WriteAsync<TMessage>(
        string queueName, TMessage message, string correlationId, CancellationToken cancellationToken)
    {
        context.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.CreateVersion7(),
            QueueName = queueName,
            Payload = JsonSerializer.Serialize(message),
            CorrelationId = correlationId,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        return Task.CompletedTask;
    }
}
