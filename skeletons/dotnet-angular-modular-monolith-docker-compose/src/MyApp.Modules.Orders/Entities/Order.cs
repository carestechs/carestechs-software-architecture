namespace MyApp.Modules.Orders.Entities;

public class Order
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    // Plain cross-module reference — never a navigation property into the
    // catalog module (adrs/dotnet/cross-module-by-id.md).
    public Guid ProductId { get; set; }

    public int Quantity { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
