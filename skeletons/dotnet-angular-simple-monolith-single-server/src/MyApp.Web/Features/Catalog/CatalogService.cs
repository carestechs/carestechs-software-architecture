using Microsoft.EntityFrameworkCore;
using MyApp.Web.Infrastructure;
using MyApp.Web.Jobs;

namespace MyApp.Web.Features.Catalog;

/// <summary>Business logic behind an interface (adrs/dotnet/service-layer-logic.md);
/// DTOs at the boundary (adrs/dotnet/dto-at-boundary.md).</summary>
public interface ICatalogService
{
    Task<IReadOnlyList<ProductDto>> ListProductsAsync(CancellationToken cancellationToken);
    Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken);
    Task<ProductDto> GetProductAsync(Guid productId, CancellationToken cancellationToken);
}

public class CatalogService(AppDbContext db, JobQueue jobs) : ICatalogService
{
    public async Task<IReadOnlyList<ProductDto>> ListProductsAsync(CancellationToken cancellationToken)
    {
        var products = await db.Products.OrderBy(p => p.CreatedAt).ToListAsync(cancellationToken);
        return products.Select(ToDto).ToList();
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var duplicate = await db.Products.AnyAsync(p => p.Sku == request.Sku, cancellationToken);
        if (duplicate)
        {
            throw new ConflictException($"A product with SKU '{request.Sku}' already exists.");
        }

        var product = new Product { Sku = request.Sku, Name = request.Name };
        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);

        // tolerable-loss side effect: enqueue the search-index sync in-process
        // (adrs/dotnet/in-process-background-jobs.md)
        await jobs.EnqueueAsync(new ProductCreatedJob(product.Id), cancellationToken);
        return ToDto(product);
    }

    public async Task<ProductDto> GetProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken)
            ?? throw new NotFoundException($"Product {productId} was not found.");
        return ToDto(product);
    }

    private static ProductDto ToDto(Product product) =>
        new(product.Id, product.Sku, product.Name, product.CreatedAt, product.SearchIndexedAt);
}
