using Microsoft.EntityFrameworkCore;
using MyApp.Contracts;
using MyApp.Modules.Catalog.DTOs;
using MyApp.Modules.Catalog.Entities;

namespace MyApp.Modules.Catalog.Services;

public class CatalogService(CatalogDbContext db) : ICatalogService, Contracts.ICatalogService
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
        return ToDto(product);
    }

    public async Task<ProductDto> GetProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken)
            ?? throw new NotFoundException($"Product {productId} was not found.");
        return ToDto(product);
    }

    public async Task<ProductSummary?> GetProductSummaryAsync(Guid productId, CancellationToken cancellationToken)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
        return product is null ? null : new ProductSummary(product.Id, product.Name);
    }

    private static ProductDto ToDto(Product product) =>
        new(product.Id, product.Sku, product.Name, product.CreatedAt);
}
