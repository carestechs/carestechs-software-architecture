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
| JWT auth: 15-min HS256 access tokens (explicit `ValidAlgorithms` allowlist, iss/aud, 60s skew, `MapInboundClaims=false`), refresh rotation with family revocation on reuse, httpOnly `SameSite=Strict` cookie scoped to `/api/auth`, CSRF header guard on refresh, deny-by-default `FallbackPolicy` | `api/jwt-bearer-auth` |
| Two-layer authorization: `[Authorize(Roles = "admin")]` on product writes, `[AllowAnonymous]` explicit on public reads; order ownership enforced in the service next to the data (404 for "not yours") with caller identity passed as explicit parameters | `api/role-based-authorization` |
| Offset pagination: shared `PaginationParams` (`[Range(1,100)]` pageSize → automatic 400 past the cap), allowlisted `sortBy` via a switch expression, `meta` reports `totalCount`/`page`/`pageSize` | `api/offset-pagination` |
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
dotnet ef database update --project src/MyApp.Modules.Identity --startup-project src/MyApp.Api --context IdentityDbContext
dotnet run --project src/MyApp.Api

# dev users are seeded automatically on startup in Development
# (admin@example.com / Admin123!, agent@example.com / Agent123!)

# tests (uses TEST_DATABASE_URL, defaults to localhost app_test — create it first:
#   docker compose exec postgres createdb -U postgres app_test
# the orders test project derives its own database from it, app_test_orders,
# and creates it automatically — parallel test assemblies never share a database)
dotnet test

# frontend (separate terminal; proxies /api to :5000)
cd client && npm install && npm start
```

## Endpoint access matrix

Every endpoint's access level is explicit (adrs/api/role-based-authorization.md):

| Endpoint | Access |
|----------|--------|
| `POST /api/auth/login` | anonymous |
| `POST /api/auth/refresh` | refresh cookie + `X-Requested-With` header (CSRF guard) |
| `GET /api/products`, `GET /api/products/{id}` | anonymous — public catalog, deliberate `[AllowAnonymous]` |
| `POST /api/products` | role `admin` |
| `POST /api/orders` | any authenticated user (`createdBy` stamped from claims) |
| `GET /api/orders/{id}` | owner or `admin` — service-layer check, 404 otherwise |
| `POST /api/auth/logout` | refresh cookie + `X-Requested-With` header — revokes the token family |
| `GET /health` | anonymous (explicit opt-out from the deny-by-default fallback policy) |

## Deliberately not demonstrated (yet)

- **The production-hardening tier** (`deployment/queue-based-decoupling`, `deployment/idempotent-queue-consumers`, `deployment/correlation-propagation`, `database/transactional-outbox`, `database/schema-per-module`, `dotnet/module-facade`) — the skeleton demonstrates the Required day-one tier; the hardening rung is adopted when pain arrives (see the profile overview).

- **An orders UI and a login UI** — the second module and the auth stack demonstrate backend rules;
  the Angular client only shows the public catalog. (A frontend would keep access tokens in memory
  only — never localStorage/sessionStorage.)
- Each module keeps an internal service interface (`Services/ICatalogService`, `Services/IOrdersService`);
  the cross-module `MyApp.Contracts.ICatalogService` is a separate, narrower surface that Orders consumes.

Additions must follow the profile's ADRs — this skeleton is held to the same constraints it demonstrates.
