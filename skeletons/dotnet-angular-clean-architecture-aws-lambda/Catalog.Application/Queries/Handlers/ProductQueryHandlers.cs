using Catalog.Application.Contracts;
using Catalog.Application.Models;
using Common.Lib.Contracts;

namespace Catalog.Application.Queries.Handlers;

public sealed class GetProductByIdQueryHandler(IProductRepository products)
    : IQueryHandler<GetProductByIdQuery, ProductContext?>
{
    public async Task<ProductContext?> HandleAsync(
        GetProductByIdQuery query, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(query.ProductId, cancellationToken);
        return product is null ? null : ProductContext.From(product);
    }
}

public sealed class ListProductsQueryHandler(IProductRepository products)
    : IQueryHandler<ListProductsQuery, IReadOnlyList<ProductContext>>
{
    public async Task<IReadOnlyList<ProductContext>> HandleAsync(
        ListProductsQuery query, CancellationToken cancellationToken)
    {
        var items = await products.ListAsync(cancellationToken);
        return items.Select(ProductContext.From).ToList();
    }
}
