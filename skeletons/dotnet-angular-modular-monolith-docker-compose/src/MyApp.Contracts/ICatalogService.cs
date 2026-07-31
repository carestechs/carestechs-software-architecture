namespace MyApp.Contracts;

/// <summary>
/// Cross-module view of the catalog module (adrs/dotnet/modular-monolith.md,
/// adrs/dotnet/cross-module-by-id.md): other modules hold plain Guid references
/// and resolve them through this contract — never through the catalog's DbContext.
/// </summary>
public interface ICatalogService
{
    Task<ProductSummary?> GetProductSummaryAsync(Guid productId, CancellationToken cancellationToken);
}

public sealed record ProductSummary(Guid Id, string Name);
