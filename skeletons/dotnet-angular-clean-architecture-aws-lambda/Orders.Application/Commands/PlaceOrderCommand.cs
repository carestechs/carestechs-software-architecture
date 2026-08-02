using Common.Lib.Contracts;
using Common.Lib.Results;

namespace Orders.Application.Commands;

public sealed record PlaceOrderCommand(Guid ProductId, int Quantity) : ICommand<Result<Guid>>;

public sealed record ConfirmOrderCommand(Guid OrderId) : ICommand<Result<Guid>>;
