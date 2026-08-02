using Orders.Domain.Models;

namespace Orders.Application.Contracts;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    void Add(Order order);
}
