using Common.Core.Messages;
using Common.Lib.Contracts;
using Microsoft.Extensions.Logging;
using Orders.Application.Events;

namespace Orders.Application.Reactors;

/// <summary>Reactors enqueue; workers dequeue (adrs/dotnet/event-driven-reactors.md,
/// adrs/deployment/queue-based-decoupling.md). The current correlation id rides
/// the message (adrs/deployment/correlation-propagation.md).</summary>
public sealed class OrderPlacedReactor(
    IQueueProvider queue,
    ICorrelationContext correlation,
    ILogger<OrderPlacedReactor> logger) : IReactor<OrderPlacedEvent>
{
    public async Task ReactAsync(OrderPlacedEvent domainEvent, CancellationToken cancellationToken)
    {
        await queue.EnqueueAsync(
            QueueNames.OrderPlaced,
            new OrderPlacedMessage(domainEvent.OrderId, domainEvent.ProductId, domainEvent.Quantity),
            correlation.CorrelationId,
            cancellationToken);
        logger.LogInformation("Enqueued OrderPlaced for {OrderId}", domainEvent.OrderId);
    }
}
