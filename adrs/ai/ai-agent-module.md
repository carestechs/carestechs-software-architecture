---
category: ai
stack: any
status: Active
requires:
  - adrs/dotnet/modular-monolith.md | adrs/python/modular-packages.md
  - adrs/dotnet/service-layer-logic.md | adrs/python/service-layer-logic.md
  - adrs/dotnet/dto-at-boundary.md | adrs/python/pydantic-at-boundary.md
conflicts_with: []
last_reviewed: 2026-08-01
---

# AI Agent as a Dedicated Module

## Decision
The AI agent is a dedicated feature module that follows all of the stack's modular monolith conventions — its own project/package, its own data mapping for AI-owned tables (conversations, messages, embeddings), its own routers/controllers, services, DTOs, and a `tools/` area — registered through the same mechanism every other module uses. The AI module accesses other modules exclusively through shared contract interfaces.

## Rationale
- Treating the AI agent as a first-class module ensures it respects the same boundaries, data ownership, and isolation rules as every other module. This prevents AI concerns from leaking into business modules and keeps the AI surface area auditable.
- Alternatives considered: embedding AI logic in a shared service/utility project (rejected — violates module ownership and makes it impossible to evolve AI independently), a separate microservice (rejected — premature for current scale; the module can be extracted later), scattering AI endpoints across existing modules (rejected — AI orchestration logic has its own lifecycle and dependencies).
- The AI module owns its own tables, keeping AI-specific data out of business module schemas.
- Cross-module references use plain UUID/Guid values, never ORM navigation properties or relationships, consistent with the cross-module-by-id family rule.

## Constraints (non-negotiable for AI)
- The AI module MUST be a first-class module of the host architecture, owning its structure and data exclusively.
- The AI module MUST own the AI tables (conversations, messages, embeddings); they MUST NOT appear in any other module's data mapping.
- Cross-module references MUST be plain UUID/Guid values — no navigation properties or ORM relationships to entities owned by other modules.
- The AI module MUST NOT contain business logic belonging to other domains. It delegates to other modules via shared contract interfaces.

**.NET mechanics:**
- Its own .csproj named `MyApp.Modules.AI` (root-namespace placeholder, not a literal name) with `Controllers/`, `Services/`, `Entities/`, `DTOs/`, and `Tools/` folders.
- Its own `AIDbContext` mapping only AI-owned entities; migrations use `--context AIDbContext`.
- Registration via an `AddAIModule()` `IServiceCollection` extension method in the API host.

**Python mechanics:**
- A package at `src/app/modules/ai/` containing `router.py`, `service.py`, `models.py`, `schemas.py`, `dependencies.py`, and a `tools/` sub-package.
- Contract interfaces consumed from `src/app/contracts/`; a router exposed for registration in the main app.
- Migrations use the module-prefix convention: slug prefixed with `ai_` (e.g., `<rev>_ai_add_conversations.py`) in the shared migration history.
