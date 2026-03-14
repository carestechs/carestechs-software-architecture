# Cross-Module References By ID Only

**Category:** dotnet
**Status:** Active
**Requires:** `adrs/dotnet/modular-monolith.md`, `adrs/dotnet/dbcontext-per-module.md`
**Conflicts with:** —

## Decision
Modules reference each other's entities exclusively by storing the foreign entity's ID as a plain `Guid` property. No EF Core navigation properties span module boundaries. When a module needs data from another module, it calls that module's service interface — never a cross-module `Include()` or join.

## Rationale
- Navigation properties across modules would create a compile-time dependency between module projects and a runtime coupling through EF's change tracker, both of which violate module boundaries.
- Alternatives considered: shared navigation properties with lazy loading (rejected — couples modules at the EF level, makes extraction to services impossible), domain events for cross-module data sync (complementary — can be added later, but the ID-only rule still applies to the data model).
- Storing a `Guid` foreign key as a plain property makes the relationship explicit without creating ORM-level coupling. The property is just data, not a tracked relationship.
- This pattern directly supports future microservice extraction: if a module becomes its own service, the ID-based references remain valid — only the data retrieval mechanism changes (from in-process service call to HTTP/gRPC call).

## Constraints (non-negotiable for AI)
- Cross-module references MUST be stored as `Guid` properties (e.g., `public Guid OwnerId { get; set; }`), NOT as navigation properties.
- NEVER add a navigation property (e.g., `public User Owner { get; set; }`) to an entity in another module.
- NEVER use `.Include()` to load an entity from another module's DbContext.
- NEVER write cross-module joins (LINQ `join` or raw SQL joins across module schemas).
- If you need data from another module, inject and call that module's service interface (e.g., `IUserService.GetByIdAsync(Guid id)`).
- The service interface MUST be defined in the shared contracts project, not in the module itself.
