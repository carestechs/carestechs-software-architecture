# AI Agent as a Dedicated Module

**Category:** ai
**Status:** Active
**Requires:** `adrs/dotnet/modular-monolith.md`, `adrs/dotnet/dbcontext-per-module.md`, `adrs/dotnet/cross-module-by-id.md`, `adrs/dotnet/thin-api-host.md`, `adrs/dotnet/service-layer-logic.md`, `adrs/dotnet/dto-at-boundary.md`
**Conflicts with:** `adrs/ai/ai-module-python.md`

## Decision
The AI agent is a dedicated feature module (`MyApp.Modules.AI`) that follows all modular monolith conventions: its own .csproj, its own `AIDbContext`, its own Controllers/Services/Entities/DTOs/Tools folder structure, and an `AddAIModule()` extension method for DI registration. The AI module accesses other modules exclusively through shared contract interfaces.

## Rationale
- Treating the AI agent as a first-class module ensures it respects the same boundaries, data ownership, and isolation rules as every other module. This prevents AI concerns from leaking into business modules and keeps the AI surface area auditable.
- Alternatives considered: embedding AI logic in a shared service project (rejected — violates module ownership and makes it impossible to evolve AI independently), a separate microservice (rejected — premature for current scale; the module can be extracted later if needed), scattering AI endpoints across existing modules (rejected — AI orchestration logic has its own lifecycle and dependencies).
- The AI module owns its own tables (conversations, messages, embeddings) via `AIDbContext`, keeping AI-specific data out of business module schemas.
- Cross-module references use plain Guids rather than navigation properties, consistent with the `cross-module-by-id` decision.

## Constraints (non-negotiable for AI)
- The AI module MUST be its own .csproj named `MyApp.Modules.AI` (substitute `MyApp` with the solution's root namespace — it is a placeholder, not a literal name).
- The AI module MUST contain `Controllers/`, `Services/`, `Entities/`, `DTOs/`, and `Tools/` folders.
- The AI module MUST define `AIDbContext` that maps only AI-owned entities (conversations, messages, embeddings).
- Cross-module references MUST be plain Guid values — no navigation properties to entities owned by other modules.
- The AI module MUST NOT contain business logic belonging to other domains. It delegates to other modules via shared contract interfaces.
- The AI module MUST expose `AddAIModule()` as an `IServiceCollection` extension method for registration in the API host.
- Migrations for the AI module MUST use `--context AIDbContext`.
