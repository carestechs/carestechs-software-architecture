using Common.Lib.Contracts;
using Common.Lib.Errors;
using Common.Lib.Results;
using Orders.Application.Contracts;

namespace Orders.Application.Commands.Handlers;

/// <summary>Runs in the SQS worker. Idempotent under redelivery: a second
/// delivery finds the order already confirmed and succeeds without effect
/// (adrs/deployment/idempotent-queue-consumers.md).</summary>
public sealed class ConfirmOrderCommandHandler(
    IOrderRepository orders,
    IUnitOfWork unitOfWork) : ICommandHandler<ConfirmOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> HandleAsync(
        ConfirmOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
        {
            return Result<Guid>.Failure(GenericErrors.NotFound("Order", command.OrderId));
        }

        if (order.Confirm())
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result<Guid>.Success(order.Id);
    }
}
