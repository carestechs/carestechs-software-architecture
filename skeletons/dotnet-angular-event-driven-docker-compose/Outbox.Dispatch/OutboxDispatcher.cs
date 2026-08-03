using System.Text.Json;
using Common.Lib.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orders.Data;

namespace Outbox.Dispatch;

/// <summary>Drains pending outbox rows to the broker
/// (adrs/database/transactional-outbox.md). Each row is stamped as soon as its
/// publish succeeds; a crash between the two redelivers on the next pass —
/// at-least-once by design, which is why consumers are idempotent
/// (adrs/deployment/idempotent-queue-consumers.md).</summary>
public sealed class OutboxDispatcher(
    OrdersDbContext context,
    IQueueProvider queue,
    ILogger<OutboxDispatcher> logger)
{
    public async Task<int> DrainOnceAsync(CancellationToken cancellationToken)
    {
        var pending = await context.OutboxMessages
            .Where(m => m.DispatchedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var row in pending)
        {
            // the payload is already serialized; JsonElement re-publishes it verbatim
            var payload = JsonSerializer.Deserialize<JsonElement>(row.Payload);
            await queue.EnqueueAsync(row.QueueName, payload, row.CorrelationId, cancellationToken);
            row.DispatchedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Dispatched outbox row {Id} to {Queue}", row.Id, row.QueueName);
        }
        return pending.Count;
    }
}
