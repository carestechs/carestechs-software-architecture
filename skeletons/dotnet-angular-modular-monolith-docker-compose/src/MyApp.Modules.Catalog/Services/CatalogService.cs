using Microsoft.EntityFrameworkCore;
using MyApp.Contracts;
using MyApp.Modules.Catalog.DTOs;
using MyApp.Modules.Catalog.Entities;

namespace MyApp.Modules.Catalog.Services;

public class CatalogService(CatalogDbContext db) : ICatalogService, Contracts.ICatalogService
{
    public async Task<(IReadOnlyList<ProductDto> Items, int TotalCount)> ListProductsAsync(
        PaginationParams pagination, CancellationToken cancellationToken)
    {
        var total = await db.Products.CountAsync(cancellationToken);
        var products = await ApplySort(db.Products, pagination.SortBy, pagination.SortDir)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);
        return (products.Select(ToDto).ToList(), total);
    }

    // Sortable columns are an allowlist — raw client input never reaches
    // ORDER BY (adrs/api/offset-pagination.md)
    private static IQueryable<Product> ApplySort(IQueryable<Product> query, string sortBy, string sortDir) =>
        (sortBy, Descending: sortDir == "desc") switch
        {
            ("createdAt", false) => query.OrderBy(p => p.CreatedAt),
            ("createdAt", true) => query.OrderByDescending(p => p.CreatedAt),
            ("name", false) => query.OrderBy(p => p.Name),
            ("name", true) => query.OrderByDescending(p => p.Name),
            ("sku", false) => query.OrderBy(p => p.Sku),
            ("sku", true) => query.OrderByDescending(p => p.Sku),
            _ => throw new BadRequestException(
                $"Unknown sortBy '{sortBy}'. Sortable: createdAt, name, sku."),
        };

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
