using MyApp.Modules.Catalog.DTOs;

namespace MyApp.Modules.Catalog.Services;

/// <summary>Module-internal service surface consumed by the module's controllers
/// (adrs/dotnet/service-layer-logic.md). Cross-module callers use MyApp.Contracts.ICatalogService.</summary>
public interface ICatalogService
{
    Task<IReadOnlyList<ProductDto>> ListProductsAsync(CancellationToken cancellationToken);
    Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken);
    Task<ProductDto> GetProductAsync(Guid productId, CancellationToken cancellationToken);
}
