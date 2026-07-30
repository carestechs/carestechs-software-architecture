# Rich Domain Entities with Factory Methods

**Category:** dotnet
**Status:** Active
**Requires:** `adrs/dotnet/clean-architecture-layers.md`
**Conflicts with:** `adrs/dotnet/service-layer-logic.md`

## Decision

Domain entities use private setters, a private parameterless constructor (for EF Core materialization), and a static `Create()` factory method for construction. Business logic that enforces entity invariants (state transitions, validation, computed state) lives inside the entity. Entities are never constructed with `new` from outside — only via the factory method.

## Rationale

- Private setters prevent external code from putting an entity into an invalid state. All state changes go through methods that enforce business rules, making invariant violations impossible at the type level.
- Alternatives considered: anemic domain model with public setters and logic in services (rejected — scatters business rules across services, making invariants hard to enforce and discover), record types for entities (rejected — record classes are reference types, but their value-based equality and `with`-expression copying are the wrong semantics for identity-based, mutable entities tracked by EF Core's change tracker).
- The static `Create()` factory method encapsulates construction logic (ID generation, default values, initial timestamps) in a single discoverable location. This is preferable to constructors because factory methods can have descriptive names and don't interact with EF Core's materialization.
- The private parameterless constructor satisfies EF Core's requirement for materialization without exposing it to application code.

## Constraints (non-negotiable for AI)

- Entity properties MUST use `private set` accessors. NEVER use `public set` on domain entity properties.
- Entities MUST have a `private` parameterless constructor for EF Core materialization.
- Entities MUST expose a `public static Create(...)` factory method for construction. NEVER construct entities with `new Entity()` outside the entity itself.
- The `Create()` method MUST generate the entity's `Id` (via `Guid.CreateVersion7()` on .NET 9+, else `Guid.NewGuid()`) and set `CreatedAt` (via `DateTimeOffset.UtcNow`, or an injected `TimeProvider` when deterministic tests need it).
- State-changing logic (e.g., `MarkProcessing()`, `Deactivate()`, `Update()`) MUST be instance methods on the entity.
- Collection navigation properties MUST use a private backing field (e.g., `private readonly List<Child> _children = new()`) with a public `IReadOnlyCollection<Child>` accessor.
- NEVER put orchestration logic (calling repositories, publishing events, coordinating multiple entities) in entities — that belongs in command handlers.
