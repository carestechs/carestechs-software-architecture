---
category: dotnet
stack: dotnet
status: Active
requires: []
conflicts_with:
  - adrs/dotnet/modular-monolith.md
  - adrs/dotnet/clean-architecture-layers.md
last_reviewed: 2026-08-02
---

# Single-Project Monolith

## Decision
The application is ONE web project (plus one test project), organized as a folder per feature (`Features/Catalog/`, `Features/Orders/` — each holding its controllers, services, entities, and DTOs). One `AppDbContext` maps all entities; EF Core migrations are the schema story. There are no module csprojs, no Contracts project, no per-module DbContexts. The service-layer and DTO-at-boundary rules still apply in full — they are what keeps the later graduation to a modular monolith mechanical.

## Rationale
- Modular ceremony has real cost: N csprojs, a contracts project, per-module migrations, and cross-module rules pay off only when multiple people or multiple domains need defended boundaries. Below that threshold the ceremony buys nothing and slows a small team down.
- An honest single project beats half-hearted modules: the observed failure mode of premature modularization is boundary rules that exist on paper and drift in practice (audits of real systems bear this out).
- Alternatives considered: starting modular "because we will grow" (rejected — graduate when growth actually arrives; the tiers of the modular profile are one `git mv` away when discipline held), a single project with no internal structure (rejected — feature folders plus the service layer are what make the eventual extraction mechanical), Clean Architecture layers in one project (rejected — layer folders without module boundaries is ceremony without the payoff).
- Navigation properties between entities are permitted — there are no module boundaries to protect yet. The DTO boundary still keeps entities out of API responses.

## Constraints (non-negotiable for AI)
- ONE web project and one test project. NEVER add module or layer csprojs under this ADR — adding them means graduating to the modular-monolith profile, not bending this one.
- Features live in folders (`Features/<Name>/`), each containing its controllers, services, entities, and DTOs. NEVER organize by technical kind at the root (a global `Controllers/` folder with twenty controllers).
- ONE `AppDbContext`; schema changes ride EF Core migrations committed with the change.
- Services hold the business logic behind interfaces, injected into thin controllers; DTOs at the API boundary — the service-layer and dto-at-boundary rules apply unchanged.
- Navigation properties between entities are allowed. When a feature is later extracted to a module, its references to other features' entities MUST be converted to plain IDs (the cross-module-by-id rule) — keep aggregates coherent per feature folder so that conversion stays tractable.
- Graduate to the modular-monolith profile when two or more of these hold: more than ~3 developers stepping on each other, clearly distinguishable domains, onboarding pain, or a feature that needs independent evolution. Graduation is a restructuring PR, not a rewrite, if the constraints above held.
