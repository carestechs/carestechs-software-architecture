using Microsoft.EntityFrameworkCore;
using Orders.Application.Contracts;
using Orders.Domain.Models;

namespace Orders.Data;

public sealed class OrderRepository(OrdersDbContext context) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Orders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public void Add(Order order) => context.Orders.Add(order);
}
