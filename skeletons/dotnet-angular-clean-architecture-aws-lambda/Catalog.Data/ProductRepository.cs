using Catalog.Application.Contracts;
using Catalog.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Data;

public sealed class ProductRepository(CatalogDbContext context) : IProductRepository
{
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken) =>
        context.Products.AnyAsync(p => p.Sku == sku, cancellationToken);

    public async Task<IReadOnlyList<Product>> ListAsync(CancellationToken cancellationToken) =>
        await context.Products.OrderBy(p => p.CreatedAt).ToListAsync(cancellationToken);

    public void Add(Product product) => context.Products.Add(product);
}
