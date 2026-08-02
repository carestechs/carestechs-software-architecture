using Common.Lib.Contracts;
using Common.Lib.Errors;
using Common.Lib.Results;
using Orders.Application.Contracts;
using Orders.Application.Events;
using Orders.Domain.Models;

namespace Orders.Application.Commands.Handlers;

public sealed class PlaceOrderCommandHandler(
    IOrderRepository orders,
    IUnitOfWork unitOfWork,
    IEventBus eventBus) : ICommandHandler<PlaceOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> HandleAsync(
        PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        if (command.ProductId == Guid.Empty)
        {
            return Result<Guid>.Failure(
                GenericErrors.Validation("Order.MissingProduct", "productId is required."));
        }
        if (command.Quantity is < 1 or > 999)
        {
            return Result<Guid>.Failure(
                GenericErrors.Validation("Order.InvalidQuantity", "quantity must be 1-999."));
        }

        var order = Order.Create(command.ProductId, command.Quantity);
        orders.Add(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // side effect via the bus — cross-module work leaves through a queue,
        // never a synchronous call into another module's Lambda
        await eventBus.PublishAsync(
            new OrderPlacedEvent(order.Id, order.ProductId, order.Quantity), cancellationToken);
        return Result<Guid>.Success(order.Id);
    }
}
