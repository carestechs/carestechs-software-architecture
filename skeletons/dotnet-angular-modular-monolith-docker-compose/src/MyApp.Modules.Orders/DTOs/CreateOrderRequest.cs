using System.ComponentModel.DataAnnotations;

namespace MyApp.Modules.Orders.DTOs;

public sealed class CreateOrderRequest
{
    public Guid ProductId { get; set; }

    [Range(1, 999)]
    public int Quantity { get; set; }
}
