using Microsoft.EntityFrameworkCore;
using MyApp.Contracts;
using MyApp.Modules.Orders.DTOs;
using MyApp.Modules.Orders.Entities;

namespace MyApp.Modules.Orders.Services;

/// <summary>Consumes the catalog module exclusively through MyApp.Contracts.ICatalogService —
/// this project does not reference MyApp.Modules.Catalog (adrs/dotnet/cross-module-by-id.md).</summary>
public class OrdersService(OrdersDbContext db, ICatalogService catalog) : IOrdersService
{
    public async Task<OrderDto> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var product = await catalog.GetProductSummaryAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException($"Product {request.ProductId} was not found.");

        var order = new Order { ProductId = request.ProductId, Quantity = request.Quantity };
        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);
        return new OrderDto(order.Id, order.ProductId, product.Name, order.Quantity, order.CreatedAt);
    }

    public async Task<OrderDto> GetOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw new NotFoundException($"Order {orderId} was not found.");

        var product = await catalog.GetProductSummaryAsync(order.ProductId, cancellationToken);
        return new OrderDto(order.Id, order.ProductId, product?.Name, order.Quantity, order.CreatedAt);
    }
}
