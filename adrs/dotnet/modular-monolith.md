---
category: dotnet
stack: dotnet
family: modular-monolith
status: Active
requires: []
conflicts_with:
  - adrs/dotnet/clean-architecture-layers.md
  - adrs/dotnet/single-project-monolith.md
  - adrs/python/modular-packages.md
last_reviewed: 2026-07-29
---

# Modular Monolith Architecture

## Decision
The system is built as a modular monolith: a single deployable unit composed of feature modules with clear boundaries, each module owning its own folder structure (Controllers, Services, Entities, DTOs). Modules communicate through shared interfaces registered in DI, never through direct project-to-project entity references.

## Rationale
- A modular monolith gives us the organizational benefits of microservices (bounded contexts, team ownership, independent evolution) without the operational complexity of distributed systems (network latency, eventual consistency, deployment orchestration).
- Alternatives considered: traditional layered monolith (rejected — leads to spaghetti dependencies over time), microservices (rejected — premature for current scale; can extract modules later if needed), vertical slice architecture (partially adopted within each module, but modules themselves are the primary boundary).
- Each module is a separate .csproj, enforcing compile-time boundary checking. The solution has one API host project (`MyApp.Api`) that references all module projects.
- This architecture supports future extraction: any module can be promoted to an independent service by replacing its in-process interface calls with HTTP/gRPC calls.

## Constraints (non-negotiable for AI)
- Every feature module MUST be its own .csproj with the naming convention `MyApp.Modules.<ModuleName>`.
- A module MUST contain its own Controllers, Services, Entities, and DTOs folders.
- Modules MUST NOT reference other module projects directly. Cross-module communication goes through interfaces defined in a shared contracts project (`MyApp.Contracts`).
- The API host project (`MyApp.Api`) is the only project that references module projects, and it does so solely for DI registration.
- No circular dependencies between modules. If two modules need each other, extract the shared concept into the contracts project.
- Each module MUST expose an `IServiceCollection` extension method (e.g., `AddCatalogModule()`) for self-registration.
