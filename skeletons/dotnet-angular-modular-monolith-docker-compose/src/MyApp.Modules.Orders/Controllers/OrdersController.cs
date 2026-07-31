using Microsoft.AspNetCore.Mvc;
using MyApp.Contracts;
using MyApp.Modules.Orders.DTOs;
using MyApp.Modules.Orders.Services;

namespace MyApp.Modules.Orders.Controllers;

/// <summary>Thin controller: validate, delegate, wrap (adrs/dotnet/service-layer-logic.md,
/// adrs/api/rest-envelope.md).</summary>
[ApiController]
[Route("api/orders")]
public class OrdersController(IOrdersService ordersService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<OrderDto>>> CreateOrder(
        CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await ordersService.CreateOrderAsync(request, cancellationToken);
        return CreatedAtAction(
            nameof(GetOrder), new { orderId = order.Id }, new ApiResponse<OrderDto>(order));
    }

    [HttpGet("{orderId:guid}")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> GetOrder(
        Guid orderId, CancellationToken cancellationToken)
    {
        var order = await ordersService.GetOrderAsync(orderId, cancellationToken);
        return Ok(new ApiResponse<OrderDto>(order));
    }
}
