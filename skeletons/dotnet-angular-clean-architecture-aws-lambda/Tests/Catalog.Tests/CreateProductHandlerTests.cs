using Catalog.Application.Commands;
using Catalog.Application.Commands.Handlers;
using Catalog.Application.Contracts;
using Catalog.Domain.Models;
using Common.Lib.Contracts;
using Common.Lib.Errors;
using Xunit;

namespace Catalog.Tests;

/// <summary>Handler tests with in-memory fakes — the Result flow is provable
/// without a database (adrs/dotnet/result-pattern-errors.md).</summary>
public class CreateProductHandlerTests
{
    private sealed class FakeRepository : IProductRepository
    {
        public List<Product> Items { get; } = [];
        public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(Items.FirstOrDefault(p => p.Id == id));
        public Task<bool> SkuExistsAsync(string sku, CancellationToken ct) =>
            Task.FromResult(Items.Any(p => p.Sku == sku));
        public Task<IReadOnlyList<Product>> ListAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Product>>(Items);
        public void Add(Product product) => Items.Add(product);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int Saves { get; private set; }
        public Task SaveChangesAsync(CancellationToken ct) { Saves++; return Task.CompletedTask; }
    }

    [Fact]
    public async Task Handle_ReturnsValidationFailure_WithoutTouchingTheStore()
    {
        var repository = new FakeRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateProductCommandHandler(repository, unitOfWork);

        var result = await handler.HandleAsync(
            new CreateProductCommand("", "Widget"), TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(0, unitOfWork.Saves);
    }

    [Fact]
    public async Task Handle_ReturnsConflict_ForDuplicateSku()
    {
        var repository = new FakeRepository();
        repository.Add(Product.Create("SKU-1", "Existing"));
        var handler = new CreateProductCommandHandler(repository, new FakeUnitOfWork());

        var result = await handler.HandleAsync(
            new CreateProductCommand("sku-1", "New"), TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task Handle_PersistsAndReturnsTheId()
    {
        var repository = new FakeRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateProductCommandHandler(repository, unitOfWork);

        var result = await handler.HandleAsync(
            new CreateProductCommand("SKU-2", "Widget"), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Single(repository.Items);
        Assert.Equal(1, unitOfWork.Saves);
    }
}
