using Microsoft.EntityFrameworkCore;
using MyApp.Contracts;
using MyApp.Modules.Orders.DTOs;
using MyApp.Modules.Orders.Entities;

namespace MyApp.Modules.Orders.Services;

/// <summary>Consumes the catalog module exclusively through MyApp.Contracts.ICatalogService —
/// this project does not reference MyApp.Modules.Catalog (adrs/dotnet/cross-module-by-id.md).</summary>
public class OrdersService(OrdersDbContext db, ICatalogService catalog) : IOrdersService
{
    public async Task<OrderDto> CreateOrderAsync(
        CreateOrderRequest request, Guid createdBy, CancellationToken cancellationToken)
    {
        var product = await catalog.GetProductSummaryAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException($"Product {request.ProductId} was not found.");

        var order = new Order
        {
            ProductId = request.ProductId,
            CreatedBy = createdBy,
            Quantity = request.Quantity,
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);
        return new OrderDto(
            order.Id, order.ProductId, product.Name, order.CreatedBy, order.Quantity, order.CreatedAt);
    }

    public async Task<OrderDto> GetOrderAsync(
        Guid orderId, Guid callerId, bool callerIsAdmin, CancellationToken cancellationToken)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
        // Ownership is enforced here, next to the data — a 404 for both "missing"
        // and "not yours" so order IDs leak nothing (adrs/api/role-based-authorization.md)
        if (order is null || (order.CreatedBy != callerId && !callerIsAdmin))
        {
            throw new NotFoundException($"Order {orderId} was not found.");
        }

        var product = await catalog.GetProductSummaryAsync(order.ProductId, cancellationToken);
        return new OrderDto(
            order.Id, order.ProductId, product?.Name, order.CreatedBy, order.Quantity, order.CreatedAt);
    }
}
