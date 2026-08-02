using Catalog.Application.Contracts;
using Catalog.Domain.Models;
using Common.Lib.Contracts;
using Common.Lib.Errors;
using Common.Lib.Results;

namespace Catalog.Application.Commands.Handlers;

/// <summary>One handler per command (adrs/dotnet/cqrs-handlers.md); explicit
/// Result flow (adrs/dotnet/result-pattern-errors.md).</summary>
public sealed class CreateProductCommandHandler(
    IProductRepository products,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateProductCommand, Result<Guid>>
{
    public async Task<Result<Guid>> HandleAsync(
        CreateProductCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Sku) || command.Sku.Length > 64)
        {
            return Result<Guid>.Failure(
                GenericErrors.Validation("Product.InvalidSku", "SKU must be 1-64 characters."));
        }
        if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Length > 200)
        {
            return Result<Guid>.Failure(
                GenericErrors.Validation("Product.InvalidName", "Name must be 1-200 characters."));
        }

        if (await products.SkuExistsAsync(command.Sku.Trim().ToUpperInvariant(), cancellationToken))
        {
            return Result<Guid>.Failure(GenericErrors.Conflict(
                "Product.DuplicateSku", $"A product with SKU '{command.Sku}' already exists."));
        }

        var product = Product.Create(command.Sku, command.Name);
        products.Add(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(product.Id);
    }
}
