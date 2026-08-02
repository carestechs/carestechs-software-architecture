using Orders.Domain.Models;

namespace Orders.Application.Models;

public sealed record OrderContext(
    Guid Id, Guid ProductId, int Quantity, string Status,
    DateTimeOffset CreatedAt, DateTimeOffset? ConfirmedAt)
{
    public static OrderContext From(Order order) => new(
        order.Id, order.ProductId, order.Quantity, order.Status,
        order.CreatedAt, order.ConfirmedAt);
}
