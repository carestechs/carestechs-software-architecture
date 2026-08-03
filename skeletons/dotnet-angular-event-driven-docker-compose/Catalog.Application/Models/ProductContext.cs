using Catalog.Domain.Models;

namespace Catalog.Application.Models;

/// <summary>Response DTO — entities never cross the boundary (adrs/dotnet/dto-at-boundary.md).</summary>
public sealed record ProductContext(Guid Id, string Sku, string Name, DateTimeOffset CreatedAt)
{
    public static ProductContext From(Product product) =>
        new(product.Id, product.Sku, product.Name, product.CreatedAt);
}
