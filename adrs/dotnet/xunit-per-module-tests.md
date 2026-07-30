---
category: dotnet
stack: dotnet
status: Active
requires: []
conflicts_with:
  - adrs/python/pytest-testing.md
last_reviewed: 2026-07-30
---

# xUnit with Per-Module Test Projects

## Decision
xUnit is the test framework for all .NET code. Test projects live under `tests/` and mirror the solution structure: one test project per module in the modular monolith (`MyApp.Modules.<Name>.Tests`), or one per layer under Clean Architecture (`<Module>.Domain.Tests`, `<Module>.Application.Tests`, `<Module>.Api.Tests`). Tests follow Arrange-Act-Assert and mock only at system boundaries. Data-access behavior is tested against a real PostgreSQL, never the EF Core InMemory provider.

## Rationale
- xUnit is the de-facto standard for modern .NET: constructor-based fixture injection matches the DI style used throughout the stack, and it is the template default for ASP.NET Core.
- Alternatives considered: NUnit (viable — mature and feature-equivalent; xUnit chosen for its DI-aligned fixture model and ecosystem momentum), MSTest (rejected — weakest ecosystem and least expressive fixtures).
- Mirroring the solution structure keeps ownership obvious: a module's tests live and move with the module, matching the per-module boundary rules of the architecture ADRs. (The TypeScript stack's equivalent decision is `adrs/typescript/vitest-colocated.md` — the two apply to different stacks and do not overlap.)
- The EF Core InMemory provider is not a relational database: it ignores constraints, transactions, and SQL translation, so it green-lights queries that fail on PostgreSQL. Testing against the real engine (e.g., Testcontainers) is the only way data-access tests earn trust.
- Mocking only at boundaries keeps tests refactor-safe: internal restructuring does not break tests that assert observable behavior.

## Constraints (non-negotiable for AI)
- All test projects MUST use xUnit and live under `tests/`, mirroring the solution: one project per module (modular monolith) or per layer (Clean Architecture).
- Tests MUST follow the Arrange-Act-Assert pattern.
- Test method names MUST describe scenario and expectation (e.g., `CreateProduct_DuplicateSku_ReturnsConflict`).
- Services and handlers MUST be unit-testable without the HTTP host — construct them directly with mocked contract interfaces.
- Mock ONLY at system boundaries: other modules' contract interfaces, external APIs, `IQueueProvider`, LLM clients (`IChatClient`). NEVER mock a module's own internal classes — test through the public surface.
- Data-access tests MUST run against a real PostgreSQL instance (e.g., Testcontainers). NEVER assert query behavior against the EF Core InMemory provider.
- API integration tests MUST exercise the real pipeline via `WebApplicationFactory<Program>` (or the module's own `Program` under Clean Architecture), not hand-wired controller instances.
- CI MUST run `dotnet test` for the whole solution on every push and pull request.

## Examples

**Violation — asserting query behavior on the InMemory provider:**
```csharp
var options = new DbContextOptionsBuilder<CatalogDbContext>()
    .UseInMemoryDatabase("test").Options; // ignores constraints and SQL translation
```

**Compliant:**
```csharp
await using var postgres = new PostgreSqlBuilder().Build(); // Testcontainers
await postgres.StartAsync();
var options = new DbContextOptionsBuilder<CatalogDbContext>()
    .UseNpgsql(postgres.GetConnectionString()).Options;
```
