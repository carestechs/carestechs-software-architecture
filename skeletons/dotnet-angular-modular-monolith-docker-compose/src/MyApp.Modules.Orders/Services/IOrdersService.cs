using MyApp.Modules.Orders.DTOs;

namespace MyApp.Modules.Orders.Services;

/// <summary>Module-internal service surface (adrs/dotnet/service-layer-logic.md).</summary>
public interface IOrdersService
{
    Task<OrderDto> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken);
    Task<OrderDto> GetOrderAsync(Guid orderId, CancellationToken cancellationToken);
}
