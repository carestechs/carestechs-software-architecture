using Common.Core.Messages;
using Common.Lib.Contracts;
using Microsoft.Extensions.Logging;
using Orders.Application.Contracts;
using Orders.Application.Events;

namespace Orders.Application.Reactors;

/// <summary>Reactors record; the dispatcher publishes; workers dequeue
/// (adrs/dotnet/event-driven-reactors.md, adrs/database/transactional-outbox.md).
/// The outbox row joins the handler's transaction, so the message exists if and
/// only if the order does. The current correlation id rides the row
/// (adrs/deployment/correlation-propagation.md).</summary>
public sealed class OrderPlacedReactor(
    IOutboxWriter outbox,
    ICorrelationContext correlation,
    ILogger<OrderPlacedReactor> logger) : IReactor<OrderPlacedEvent>
{
    public async Task ReactAsync(OrderPlacedEvent domainEvent, CancellationToken cancellationToken)
    {
        await outbox.WriteAsync(
            QueueNames.OrderPlaced,
            new OrderPlacedMessage(domainEvent.OrderId, domainEvent.ProductId, domainEvent.Quantity),
            correlation.CorrelationId,
            cancellationToken);
        logger.LogInformation("Outboxed OrderPlaced for {OrderId}", domainEvent.OrderId);
    }
}
