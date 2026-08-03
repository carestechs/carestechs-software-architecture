using Catalog.Domain.Models;

namespace Catalog.Application.Contracts;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken);
    Task<IReadOnlyList<Product>> ListAsync(CancellationToken cancellationToken);
    void Add(Product product);
}
