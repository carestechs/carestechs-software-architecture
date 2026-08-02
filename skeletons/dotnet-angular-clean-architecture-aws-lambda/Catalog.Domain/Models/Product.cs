namespace Catalog.Domain.Models;

/// <summary>Rich entity (adrs/dotnet/rich-domain-entities.md): private setters,
/// a Create() factory that enforces invariants, business rules on the entity.</summary>
public class Product
{
    private Product() { } // EF Core

    public Guid Id { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    public static Product Create(string sku, string name)
    {
        if (string.IsNullOrWhiteSpace(sku) || sku.Length > 64)
        {
            throw new ArgumentException("SKU must be 1-64 characters.", nameof(sku));
        }
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
        {
            throw new ArgumentException("Name must be 1-200 characters.", nameof(name));
        }

        return new Product
        {
            Id = Guid.CreateVersion7(),
            Sku = sku.Trim().ToUpperInvariant(), // invariant lives here, not in handlers
            Name = name.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }
}
