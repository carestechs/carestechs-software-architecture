namespace Orders.Domain.Models;

public static class OrderStatus
{
    public const string Placed = "placed";
    public const string Confirmed = "confirmed";
}

/// <summary>Rich entity (adrs/dotnet/rich-domain-entities.md). ProductId is a
/// plain Guid — no navigation into the catalog module
/// (adrs/dotnet/cross-module-by-id.md).</summary>
public class Order
{
    private Order() { } // EF Core

    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public string Status { get; private set; } = OrderStatus.Placed;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }

    public static Order Create(Guid productId, int quantity)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("ProductId is required.", nameof(productId));
        }
        if (quantity is < 1 or > 999)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be 1-999.");
        }

        return new Order
        {
            Id = Guid.CreateVersion7(),
            ProductId = productId,
            Quantity = quantity,
            Status = OrderStatus.Placed,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>Idempotent by design: confirming a confirmed order is a no-op —
    /// the at-least-once pipeline may deliver the message twice
    /// (adrs/deployment/idempotent-queue-consumers.md).</summary>
    public bool Confirm()
    {
        if (Status == OrderStatus.Confirmed)
        {
            return false;
        }

        Status = OrderStatus.Confirmed;
        ConfirmedAt = DateTimeOffset.UtcNow;
        return true;
    }
}
