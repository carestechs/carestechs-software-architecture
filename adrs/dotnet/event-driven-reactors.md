# Event-Driven Side Effects via Reactors

**Category:** dotnet
**Status:** Active
**Requires:** `adrs/dotnet/cqrs-handlers.md`
**Conflicts with:** —

## Decision

Side effects triggered by domain operations are modeled as events processed by Reactor classes. Command handlers publish events via an in-process `IEventBus`. Reactors implement `IReactors<TEvent>` and are registered in DI. Events are defined in `Common.Core` when they cross module boundaries. Reactors can enqueue work to external queues for asynchronous processing by worker services.

## Rationale

- Reactors decouple the primary operation (e.g., "register an image") from its side effects (e.g., "enqueue preprocessing", "notify monitoring"). The command handler focuses on its core responsibility; side effects are handled independently and can fail without affecting the primary operation.
- Alternatives considered: calling side-effect logic directly from command handlers (rejected — creates tight coupling between modules and makes handlers grow unbounded), MediatR notifications (rejected — same decoupling benefit but adds a library dependency; our `IEventBus` is simpler and more transparent), domain events on entities (rejected — entities should not depend on infrastructure; events are published from handlers).
- Events that cross module boundaries are defined in `Common.Core` as shared contracts. Module-internal events stay within the module.
- Reactors that need to delegate work asynchronously enqueue messages via `IQueueProvider`, following the pattern of decoupling via queues. (`IEventBus`, `IReactors<TEvent>`, and `IQueueProvider` are defined in `Common.Lib/Contracts/` and implemented in `Common.Providers`; the plural name `IReactors<TEvent>` is historical — each implementation is a single reactor.)

## Constraints (non-negotiable for AI)

- Side effects MUST be modeled as event classes implementing `IEvent`.
- Events that cross module boundaries MUST be defined in `Common.Core/Events/`.
- Reactors MUST implement `IReactors<TEvent>` with a `HandleAsync` method.
- Reactors MUST be registered as scoped services in `Program.cs`: `builder.Services.AddScoped<IReactors<SomeEvent>, SomeReactor>()`.
- Command handlers MUST publish events via `IEventBus.PublishAsync()` — NEVER call reactor logic directly from a handler.
- Multiple reactors MAY subscribe to the same event type. Each is invoked independently.
- The `IEventBus` implementation MUST invoke each reactor in isolation: catch and log a reactor's exception without failing the publishing command handler or blocking other reactors. Side effects requiring guaranteed execution belong on a queue, not the in-process bus.
- Reactors that trigger cross-module work MUST enqueue messages via `IQueueProvider` rather than calling the other module's handlers directly.
- NEVER put primary business logic in reactors — they handle secondary/side-effect concerns only.
