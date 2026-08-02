using Catalog.Application.Models;
using Common.Lib.Contracts;

namespace Catalog.Application.Queries;

/// <summary>Query handlers return nullable DTOs; the Api layer maps null to 404
/// (family-B convention).</summary>
public sealed record GetProductByIdQuery(Guid ProductId) : IQuery<ProductContext?>;

public sealed record ListProductsQuery : IQuery<IReadOnlyList<ProductContext>>;
