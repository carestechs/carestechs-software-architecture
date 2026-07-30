---
category: dotnet
stack: dotnet
status: Active
requires: []
conflicts_with:
  - adrs/dotnet/modular-monolith.md
last_reviewed: 2026-07-29
---

# Clean Architecture with Domain/Application/Data/Api Layers

## Decision

Each feature module is split into four separate projects following Clean Architecture: `Module.Domain` (entities, enums, value objects), `Module.Application` (commands, queries, handlers, repository contracts, DTOs), `Module.Data` (DbContext, EF Core repositories), and `Module.Api` (endpoints, DI registration, hosting). Dependencies flow inward: Api → Application + Data, Data → Domain + Application, Application → Domain. Shared contracts and events live in `Common.Core` and `Common.Lib`.

## Rationale

- Clean Architecture enforces dependency inversion at the project reference level. The Domain layer has zero dependencies — it is pure business logic. The Application layer depends only on Domain and shared contracts. Data and Api are infrastructure concerns pushed to the outer ring.
- Alternatives considered: modular monolith with single .csproj per module (rejected — mixes domain, data access, and HTTP concerns in one project, making it harder to swap infrastructure), vertical slice architecture (rejected — doesn't enforce layering, making it easy to couple domain logic to EF Core).
- Each module is independently deployable as its own Lambda or service. Modules never share a single API host — each module owns its own `Program.cs` and deployment pipeline.
- This structure supports the CQRS pattern naturally: commands and queries live in the Application layer alongside their handlers, with clear separation from the data access implementation.

## Constraints (non-negotiable for AI)

- Every feature module MUST consist of four projects: `<Module>.Domain`, `<Module>.Application`, `<Module>.Data`, and `<Module>.Api`.
- `<Module>.Domain` MUST have zero project references — no dependencies on EF Core, ASP.NET, or any infrastructure package.
- `<Module>.Application` MUST reference only `<Module>.Domain` and shared contract libraries (`Common.Lib`, `Common.Core`). NEVER reference `<Module>.Data` or EF Core from Application.
- `<Module>.Data` MUST reference `<Module>.Domain` and `<Module>.Application` (for repository interface implementations).
- `<Module>.Api` is the composition root: it references `<Module>.Application`, `<Module>.Data`, and infrastructure providers.
- Repository interfaces MUST be defined in `<Module>.Application/Contracts/`. Implementations MUST be in `<Module>.Data/`.
- Shared-library split: `Common.Lib` holds domain-agnostic technical contracts and patterns (`ICommand`/`IQuery`/handler interfaces, `IEventBus`, `IReactors<TEvent>`, `IUnitOfWork`, `Result`/`Error`, provider interfaces such as `ISecretsProvider`, `IParametersProvider`, `IQueueProvider`); `Common.Core` holds cross-module business contracts (events crossing module boundaries, cross-module service interfaces). Rule: a type that names a business concept goes in `Common.Core`; reusable plumbing goes in `Common.Lib`. Neither contains implementations — those live in `Common.Providers`.
- NEVER place business logic in the Data or Api layers.
