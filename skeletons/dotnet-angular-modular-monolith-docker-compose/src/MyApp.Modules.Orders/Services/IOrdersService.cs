using MyApp.Modules.Orders.DTOs;

namespace MyApp.Modules.Orders.Services;

/// <summary>Module-internal service surface (adrs/dotnet/service-layer-logic.md).
/// Caller identity arrives as explicit parameters — never resolved from ambient
/// state inside the service (adrs/api/role-based-authorization.md).</summary>
public interface IOrdersService
{
    Task<OrderDto> CreateOrderAsync(
        CreateOrderRequest request, Guid createdBy, CancellationToken cancellationToken);

    Task<OrderDto> GetOrderAsync(
        Guid orderId, Guid callerId, bool callerIsAdmin, CancellationToken cancellationToken);
}
