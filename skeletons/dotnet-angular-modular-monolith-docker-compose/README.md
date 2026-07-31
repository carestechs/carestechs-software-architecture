# Golden Skeleton: dotnet-angular-modular-monolith-docker-compose

A minimal, **buildable** instance of `profiles/dotnet-angular-modular-monolith-docker-compose.md`.
Its job is to prove, in CI, that the profile's ADRs compose into a solution that builds, tests,
and ships. If a catalog change breaks this skeleton, the change — not the skeleton — is suspect.

## What it demonstrates

| Area | ADRs exercised |
|------|----------------|
| Modular monolith: `MyApp.Modules.Catalog` and `MyApp.Modules.Orders` as separate csprojs, `MyApp.Contracts` for cross-module interfaces, thin `MyApp.Api` host (`Program.cs` + `Infrastructure/`) with `AddCatalogModule()` + `AddOrdersModule()` | `dotnet/modular-monolith`, `dotnet/thin-api-host` |
| `CatalogDbContext` / `OrdersDbContext` each mapping only their module's entities; per-module EF migrations generated with `--context` | `dotnet/dbcontext-per-module` |
| Thin controller → `CatalogService` (scoped, interface-injected); DTOs in `DTOs/`, entities never leave the module | `dotnet/service-layer-logic`, `dotnet/dto-at-boundary` |
| Cross-module by id: `Order.ProductId` is a plain `Guid` (no navigation property, no join); `OrdersService` resolves it through `MyApp.Contracts.ICatalogService` — the Orders csproj references only `MyApp.Contracts`, never the Catalog module | `dotnet/cross-module-by-id` |
| Async end-to-end: `Task<ActionResult<T>>`, `Async` suffixes, `CancellationToken` forwarded | `dotnet/async-all-the-way` |
| `{ data, meta }` envelope (`ApiResponse<T>` / `ApiListResponse<T>`), camelCase JSON | `api/rest-envelope` |
| `AddProblemDetails()` + `IExceptionHandler` in the host's `Infrastructure/`; typed `NotFoundException`/`ConflictException`; automatic `ValidationProblemDetails` | `dotnet/rfc7807-errors` |
| `Guid.CreateVersion7()` PKs, `DateTimeOffset.UtcNow`, `UseSnakeCaseNamingConvention()` (`products`, `ix_products_sku`) | `database/uuid-primary-keys`, `database/timestamptz-always`, `database/snake-case-naming` |
| `ILogger<T>` with message templates, JSON console outside Development, request-ID scope middleware | `dotnet/structured-logging` |
| Enforcement wired into the build: BannedApiAnalyzers (`.Result`, `.Wait`, `Console`, `DateTime.Now`) as errors, CS4014/CA1849 errors | enforcement layer |
| xUnit (v3) per-module test projects (`Catalog.Tests`, `Orders.Tests`); `WebApplicationFactory<Program>` against a real PostgreSQL | `dotnet/xunit-per-module-tests` |
| Angular 20: standalone components (no `standalone: true` boilerplate), separate `.html` templates, `styles: []`, signals for state, `loadComponent` lazy route, Tailwind v4 CSS-first via PostCSS | `angular/standalone-components`, `angular/separate-template-file`, `angular/signals-state`, `angular/tailwind-no-css` |
| Multi-stage Dockerfiles (SDK → aspnet runtime; Node → nginx), dev-infra vs prod-app compose split, nginx SPA proxy | `deployment/docker-multi-stage-builds`, `deployment/local-dev-compose`, `deployment/container-per-process`, `deployment/nginx-spa-proxy`, `deployment/env-connection-urls` |
| CI: dotnet build+test (PostgreSQL service), Angular production build, docker builds | `deployment/github-actions-ci` |

## Run it locally

```bash
# infrastructure
docker compose up -d

# backend (http://localhost:5000; applies EF migration on the test database only via tests)
dotnet ef database update --project src/MyApp.Modules.Catalog --startup-project src/MyApp.Api --context CatalogDbContext
dotnet ef database update --project src/MyApp.Modules.Orders --startup-project src/MyApp.Api --context OrdersDbContext
dotnet run --project src/MyApp.Api

# tests (uses TEST_DATABASE_URL, defaults to localhost app_test — create it first:
#   docker compose exec postgres createdb -U postgres app_test
# the orders test project derives its own database from it, app_test_orders,
# and creates it automatically — parallel test assemblies never share a database)
dotnet test

# frontend (separate terminal; proxies /api to :5000)
cd client && npm install && npm start
```

## Deliberately not demonstrated (yet)

- **JWT auth / role-based authorization** (`api/jwt-bearer-auth`, `api/role-based-authorization`) — endpoints are anonymous.
- **Background workers** — the profile has no queue ADR in its Required tier; nothing to demonstrate here yet.
- **Offset pagination** (Optional tier) — the list endpoint returns all rows with `meta.totalCount`.
- **An orders UI** — the second module exists to demonstrate backend module boundaries; the Angular
  client only shows the catalog.
- Each module keeps an internal service interface (`Services/ICatalogService`, `Services/IOrdersService`);
  the cross-module `MyApp.Contracts.ICatalogService` is a separate, narrower surface that Orders consumes.

Additions must follow the profile's ADRs — this skeleton is held to the same constraints it demonstrates.
