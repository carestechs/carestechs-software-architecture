---
category: dotnet
stack: dotnet
status: Active
requires:
  - adrs/dotnet/clean-architecture-layers.md | adrs/dotnet/modular-monolith.md
conflicts_with: []
last_reviewed: 2026-07-31
---

# Module Facade (One Public Surface Per Module)

## Decision
Each module that other modules consume exposes exactly ONE public facade interface, `I<Module>ModuleApi`, in its Application layer. The facade's operations are shaped by consumer use cases and return snapshot records — never domain entities. Every other contract in the module (repositories, per-purpose lookups, units of work) is `internal`. Consumers reference the facade and nothing else.

## Rationale
- Without a single named surface, cross-module access grows one convention per contributor: some consumers use the owning module's repositories, some add per-purpose lookup interfaces, some reach into foreign DbContexts. A real-world audit of exactly this drift found ~40 boundary violations across ~70 cross-module references. One facade per module ends the ambiguity — there is exactly one legal door.
- Snapshot records at the facade keep the module's domain model private; consumers get the minimum fields their use case needs, so internal refactors don't ripple.
- The facade is already service-shaped: promoting the module to its own deployable later means swapping the in-process implementation for an HTTP client behind the same interface — consumers do not change.
- Alternatives considered: per-purpose contract interfaces (rejected — they multiply per consumer and enforce nothing the facade doesn't), direct repository sharing (rejected — leaks persistence shape and entity types across the boundary), full read-copy projections via events (deferred — correct for read-scale or microservice extraction pressure, an order of magnitude more machinery).

## Constraints (non-negotiable for AI)
- ONE facade per module, named `I<Module>ModuleApi`, living in `<Module>.Application`. NEVER add a second public cross-module contract to the same module.
- This ADR refines `cross-module-by-id`'s contract placement: the facade IS the module's cross-module contract. Do NOT duplicate facade interfaces in the shared contracts project — shared projects keep only cross-module events and truly shared value types.
- Facade operations return snapshot records defined next to the facade. NEVER return domain entities, EF entities, or types from the module's internal layers.
- Shape operations by consumer use case (e.g., `GetUserSnapshotAsync` returning id + status + display name) — not by CRUD symmetry with internal entities.
- Tenant identifiers cross the facade as primitive `string`/`Guid` parameters — consumers must not need the owning module's value-object types.
- Every facade operation takes a `CancellationToken`.
- All non-facade contracts in the module are `internal`. If a consumer "just needs" a repository, the answer is a new facade operation, not a public repository.
- Facade implementations MUST NOT call other modules' facades in a synchronous chain deeper than one hop — fan-out belongs to events/queues.
