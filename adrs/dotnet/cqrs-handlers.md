---
category: dotnet
stack: dotnet
status: Active
requires:
  - adrs/dotnet/clean-architecture-layers.md
conflicts_with:
  - adrs/dotnet/service-layer-logic.md
last_reviewed: 2026-07-29
---

# CQRS with Command and Query Handlers

## Decision

Business logic is organized as Commands (write operations) and Queries (read operations), each with a dedicated Handler class. Commands implement `ICommand<TResult>` and are processed by `ICommandHandler<TCommand, TResult>`. Queries implement `IQuery<TResult>` and are processed by `IQueryHandler<TQuery, TResult>`. Handlers are registered as scoped services and injected directly into Minimal API endpoint delegates. No MediatR — handlers are concrete classes resolved from DI.

## Rationale

- CQRS provides a natural separation between reads and writes. Command handlers orchestrate domain operations and side effects (events). Query handlers perform optimized reads and return DTOs. This makes each operation's intent explicit and independently testable.
- Alternatives considered: MediatR (rejected — adds indirection and a pipeline abstraction with no clear benefit at current scale; direct DI injection is simpler and more debuggable), service layer with mixed read/write methods (rejected — methods accumulate and blur the intent boundary), vertical slice with handlers but no shared interface (rejected — shared interfaces enable consistent patterns across modules).
- Commands and queries are thin record types that carry only the data needed for the operation. Handlers receive them, execute logic, and return results. This makes each operation a self-contained unit.
- Handlers are registered individually in `Program.cs` — no assembly scanning. This keeps the composition root explicit and discoverable.

## Constraints (non-negotiable for AI)

- Write operations MUST be modeled as `record` types implementing `ICommand<TResult>`.
- Read operations MUST be modeled as `record` types implementing `IQuery<TResult>`.
- Each command/query MUST have exactly one handler class implementing the corresponding `ICommandHandler` or `IQueryHandler` interface.
- Handler interfaces MUST define `Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken)` (same shape for queries); endpoint delegates invoke `handler.HandleAsync(...)` directly.
- Commands declare their result through the marker interface, including the `Result` wrapper: e.g., `record CreateEntityCommand(...) : ICommand<Result<Guid>>` (see `result-pattern-errors.md`).
- Command handlers MUST live in `<Module>.Application/Commands/Handlers/`. Query handlers MUST live in `<Module>.Application/Queries/Handlers/`.
- Command and query records MUST live in `<Module>.Application/Commands/` and `<Module>.Application/Queries/` respectively.
- Handlers MUST be registered as scoped services in `Program.cs` — NEVER use assembly scanning or MediatR.
- Handlers MUST be injected directly into endpoint delegates. NEVER create a mediator or dispatcher abstraction.
- Query handlers MUST return response DTOs — the `*Context` record types in `<Module>.Application/Models/` — NEVER domain entities.

## Examples

**Violation — mixed read/write service and a dispatcher abstraction:**
```csharp
public class OrderService // reads and writes blur together
{
    public async Task<Guid> CreateOrder(CreateOrderRequest r) { /* ... */ }
    public async Task<List<OrderDto>> GetOrders() { /* ... */ }
}
await _mediator.Send(new CreateOrderCommand(customerId)); // MediatR-style dispatch
```

**Compliant:**
```csharp
public sealed record CreateOrderCommand(Guid CustomerId) : ICommand<Result<Guid>>;

public sealed class CreateOrderCommandHandler
    : ICommandHandler<CreateOrderCommand, Result<Guid>> { /* ... */ }

app.MapPost("/orders", async (CreateOrderRequest req,
    CreateOrderCommandHandler handler, CancellationToken ct) =>
    ToResponse(await handler.HandleAsync(new CreateOrderCommand(req.CustomerId), ct)));
```
