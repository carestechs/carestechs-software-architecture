---
category: dotnet
stack: dotnet
status: Active
requires:
  - adrs/dotnet/modular-monolith.md
conflicts_with: []
last_reviewed: 2026-07-29
---

# DbContext Per Module

## Decision
Each feature module owns its own DbContext that maps only that module's entities. There is no shared or "master" DbContext. EF Core migrations are generated and applied per module, scoped to that module's table set. All modules share the default schema by default; a schema-per-module layout is an acceptable alternative — pick one approach per solution and use it consistently.

## Rationale
- A DbContext per module enforces data ownership at the ORM level. A module cannot accidentally query or modify another module's tables because those tables simply do not exist in its DbContext.
- Alternatives considered: single shared DbContext (rejected — creates a god object, encourages cross-module joins, makes independent module evolution impossible), CQRS with separate read/write contexts (deferred — can be layered on later within a module if needed).
- Per-module migrations allow each module to evolve its schema independently without merge conflicts in a shared migration history.
- This aligns with the modular monolith boundary: each module owns its data, just as it owns its services and controllers.

## Constraints (non-negotiable for AI)
- Each module MUST define its own DbContext class (e.g., `CatalogDbContext`).
- A module's DbContext MUST only contain `DbSet<>` properties for entities owned by that module.
- NEVER create a shared or application-wide DbContext that spans multiple modules.
- Migrations MUST be generated per module using the `--context` flag to target the correct DbContext.
- Each module's DbContext MUST be registered in that module's `Add<ModuleName>Module()` extension method.
- All modules share the same physical database and connection string, but each DbContext maps only its own tables.
