using Common.Lib.Contracts;
using Common.Lib.Results;

namespace Catalog.Application.Commands;

public sealed record CreateProductCommand(string Sku, string Name) : ICommand<Result<Guid>>;
