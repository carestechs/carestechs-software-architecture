---
category: dotnet
stack: dotnet
status: Active
requires:
  - adrs/dotnet/modular-monolith.md | adrs/dotnet/clean-architecture-layers.md
conflicts_with: []
last_reviewed: 2026-07-29
---

# Cross-Module References By ID Only

## Decision
Modules reference each other's entities exclusively by storing the foreign entity's ID as a plain `Guid` property. No EF Core navigation properties span module boundaries. When a module needs data from another module, it calls a contract interface exposed by that module — never a cross-module `Include()` or join. This rule applies to any modular architecture: modular monolith (service interfaces in the shared contracts project) and Clean Architecture modules (contracts in `Common.Core`) alike.

## Rationale
- Navigation properties across modules would create a compile-time dependency between module projects and a runtime coupling through EF's change tracker, both of which violate module boundaries.
- Alternatives considered: shared navigation properties with lazy loading (rejected — couples modules at the EF level, makes extraction to services impossible), domain events for cross-module data sync (complementary — can be added later, but the ID-only rule still applies to the data model).
- Storing a `Guid` foreign key as a plain property makes the relationship explicit without creating ORM-level coupling. The property is just data, not a tracked relationship.
- This pattern directly supports future microservice extraction: if a module becomes its own service, the ID-based references remain valid — only the data retrieval mechanism changes (from in-process service call to HTTP/gRPC call).

## Constraints (non-negotiable for AI)
- Cross-module references MUST be stored as `Guid` properties (e.g., `public Guid OwnerId { get; set; }` — or with a private setter under rich domain entities), NOT as navigation properties.
- NEVER add a navigation property (e.g., `public User Owner { get; set; }`) to an entity in another module.
- NEVER use `.Include()` to load an entity from another module's DbContext.
- NEVER write cross-module joins (LINQ `join` or raw SQL joins across module schemas).
- If you need data from another module, inject and call a contract interface exposed by that module (e.g., `IUserService.GetByIdAsync(Guid id)`), or consume events it publishes.
- The contract interface MUST be defined in the shared contracts project (e.g., `MyApp.Contracts` or `Common.Core`), not in the module itself.

## Examples

**Violation — navigation property across a module boundary:**
```csharp
public class Order
{
    public Guid Id { get; set; }
    public User Owner { get; set; } // entity owned by the Identity module
}
var orders = await _db.Orders.Include(o => o.Owner).ToListAsync(ct);
```

**Compliant:**
```csharp
public class Order
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; } // plain ID reference
}
var order = await _db.Orders.FirstAsync(o => o.Id == id, ct);
var owner = await _identityService.GetByIdAsync(order.OwnerId, ct); // contract interface
```
