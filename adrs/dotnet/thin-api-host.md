---
category: dotnet
stack: dotnet
status: Active
requires:
  - adrs/dotnet/modular-monolith.md | adrs/dotnet/clean-architecture-layers.md
conflicts_with: []
last_reviewed: 2026-07-29
---

# Thin API Host

## Decision
The API host project (e.g., `MyApp.Api`) contains only `Program.cs` with DI registration and the middleware pipeline. It has no controllers, no services, and no business logic. Each module registers itself via an `IServiceCollection` extension method called from `Program.cs`.

## Rationale
- A thin API host ensures the host project is a composition root and nothing more. It wires modules together but contains no behavior of its own, keeping the deployment entry point stable and low-churn.
- Alternatives considered: placing shared middleware in the host (accepted only for truly cross-cutting concerns like global error handling, CORS, authentication), placing controllers in the host (rejected — controllers belong to the module that owns the feature).
- This pattern makes it trivial to see what modules are active: `Program.cs` reads as a clear list of `Add<Module>Module()` and `Use<Module>Module()` calls.
- If a module is removed, the host project requires only the removal of its registration call and project reference — no business logic cleanup.

## Constraints (non-negotiable for AI)
- The API host project MUST contain only `Program.cs`, configuration files (`appsettings.json`, `launchSettings.json`), and an optional `Infrastructure/` folder for cross-cutting host plumbing (the global `IExceptionHandler`, ProblemDetails wiring, custom middleware).
- NEVER place controllers, services, entities, DTOs, or business logic in the API host project.
- Each module MUST provide an `Add<ModuleName>Module(this IServiceCollection services)` extension method for DI registration.
- Each module MAY provide a `Use<ModuleName>Module(this IApplicationBuilder app)` extension method for middleware registration if needed.
- `Program.cs` MUST call each module's registration method explicitly — no assembly scanning magic for module discovery.
- Cross-cutting middleware (authentication, CORS, global error handling, request logging) is the only logic allowed in the host — wired in `Program.cs`, with implementations in the host's `Infrastructure/` folder.

## Examples

**Violation — controller living in the host project:**
```csharp
// MyApp.Api/Controllers/ReportsController.cs
[ApiController]
public class ReportsController : ControllerBase { /* belongs to a module */ }
```

**Compliant:**
```csharp
// MyApp.Api/Program.cs — composition root only
builder.Services.AddCatalogModule(builder.Configuration);
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddAIModule(builder.Configuration);
```
