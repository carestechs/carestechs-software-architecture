using Common.Lib.Contracts;
using Orders.Application.Contracts;
using Orders.Application.Models;

namespace Orders.Application.Queries;

public sealed record GetOrderByIdQuery(Guid OrderId) : IQuery<OrderContext?>;

public sealed class GetOrderByIdQueryHandler(IOrderRepository orders)
    : IQueryHandler<GetOrderByIdQuery, OrderContext?>
{
    public async Task<OrderContext?> HandleAsync(
        GetOrderByIdQuery query, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(query.OrderId, cancellationToken);
        return order is null ? null : OrderContext.From(order);
    }
}
