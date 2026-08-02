# Stack Profile: .NET + Angular Simple Monolith (Single Server)

**Status:** Active
**Assumes:** .NET 10+, Angular 20+, PostgreSQL, EF Core 10+ (with EF migrations), Docker Compose on one server

## Golden Skeleton

A buildable reference implementation lives at
[`skeletons/dotnet-angular-simple-monolith-single-server/`](../skeletons/dotnet-angular-simple-monolith-single-server/)
— one web project (Features folders, one `AppDbContext`), the in-process job pipeline, and the
SPA-served-by-API Dockerfile, with CI building and testing it against real PostgreSQL on every
push and pull request.

## Overview

A curated set of ADRs for building a small product as ONE web project serving both its API and its Angular SPA from a single container, next to a PostgreSQL container, on one server. This is the catalog's entry rung: the fastest build-debug-deploy loop the catalog endorses, with the cross-cutting discipline (envelope, Problem Details, JWT auth, UUID/timestamptz, structured logging, CI) kept fully intact — stripping architecture never means stripping discipline.

What this profile deliberately does NOT have: module projects, a contracts package, queues or an outbox, per-module schemas, managed identity, or any AWS machinery. Background work runs in-process with honest loss semantics. The SPA is served by the API itself — one origin, no CORS, no nginx container.

**The graduation path is the point.** The service-layer and DTO rules here are exactly the ones the modular-monolith profile requires, so when growth arrives (more than ~3 developers stepping on each other, clearly distinguishable domains, onboarding pain), moving to `dotnet-angular-modular-monolith-docker-compose` is a restructuring PR — feature folders become module csprojs — not a rewrite. That profile's Recommended tier then carries the next rung (queues, outbox, enforced boundaries), and the AWS profiles the rungs beyond.

---

## Solution Structure

```
MyApp/
├── MyApp.sln
├── Directory.Build.props                  # net10.0, analyzers, BannedSymbols
├── .editorconfig
│
├── src/MyApp.Web/                         # THE application — one web project
│   ├── Program.cs                         # DI, auth, pipeline, SPA fallback
│   ├── AppDbContext.cs                    # one context, all entities
│   ├── Migrations/                        # EF Core migrations
│   ├── Infrastructure/                    # exception handler, correlation, auth helpers
│   ├── Features/
│   │   ├── Catalog/                       # folder per feature: controllers, services,
│   │   │   ├── CatalogController.cs       #   entities, DTOs live together
│   │   │   ├── CatalogService.cs
│   │   │   ├── Product.cs
│   │   │   └── Dtos.cs
│   │   ├── Orders/
│   │   └── Identity/                      # users, login/refresh/logout
│   ├── Jobs/                              # BackgroundService + bounded Channel<T>
│   └── wwwroot/                           # Angular production bundle (built in Docker)
│
├── client/                                # Angular workspace (dev server proxies /api)
│
├── tests/MyApp.Web.Tests/                 # one test project, WebApplicationFactory vs real PG
│
├── Dockerfile                             # Node stage builds SPA → aspnet runtime + wwwroot
├── docker-compose.yml                     # dev: postgres only
└── docker-compose.prod.yml                # prod: app container + postgres, one server
```

---

## Required (core to this stack — do not omit)

These ADRs define the fundamental architecture. Removing any of them breaks the coherence of the stack.

<!-- generated from profiles/profiles.toml — edit the manifest and run scripts/generate_profiles.py -->
| ADR | Summary | Depends On |
|-----|---------|-------------|
| `adrs/dotnet/single-project-monolith.md` | ONE web project, folder per feature, one `AppDbContext` with EF migrations. Service-layer + DTO rules apply in full — graduation to modules stays a restructuring PR. | — |
| `adrs/dotnet/service-layer-logic.md` | Controllers are thin. All business logic lives in service classes. | `single-project-monolith` |
| `adrs/dotnet/dto-at-boundary.md` | Never expose EF entities via API. Mapping happens in service layer. | `service-layer-logic` |
| `adrs/dotnet/async-all-the-way.md` | All I/O uses async/await. Async suffix on service methods. | — |
| `adrs/dotnet/rfc7807-errors.md` | RFC 7807 Problem Details for all errors. Global exception handler. | — |
| `adrs/api/rest-envelope.md` | All responses wrapped in `{ data, meta }` envelope. | — |
| `adrs/api/jwt-bearer-auth.md` | JWT Bearer tokens. Short-lived access + rotated refresh. | — |
| `adrs/deployment/docker-multi-stage-builds.md` | All components packaged as Docker images with multi-stage builds. `dotnet/aspnet` final stage for backend. | — |
| `adrs/deployment/env-connection-urls.md` | All config via env vars. External services via connection URLs. Strongly-typed `IConfiguration` sections validate at startup. | — |
| `adrs/deployment/local-dev-compose.md` | `docker-compose.yml` for local infra, `docker-compose.prod.yml` for app services on shared network. | `docker-multi-stage-builds`, `env-connection-urls` |
| `adrs/deployment/spa-served-by-api.md` | SPA bundle built in the Dockerfile's Node stage, served from the API's `wwwroot` with an `index.html` fallback after API routes. One container, one origin, no CORS. | `docker-multi-stage-builds` |

## Recommended (strong defaults — can be swapped with noted alternatives)

These are battle-tested defaults. You can swap them, but you should have a good reason.

<!-- generated from profiles/profiles.toml — edit the manifest and run scripts/generate_profiles.py -->
| ADR | Summary | Alternative |
|-----|---------|-------------|
| `adrs/dotnet/in-process-background-jobs.md` | Bounded `Channel<T>` + `BackgroundService` for tolerable-loss work; Hangfire on PostgreSQL when jobs must survive restarts. One background-work model per system — a broker means graduating. | `queue-based-decoupling` with a real broker (the next rung) |
| `adrs/dotnet/structured-logging.md` | ILogger<T> with message templates. JSON output + correlation IDs in production. | Serilog as host provider (compatible) |
| `adrs/dotnet/xunit-per-module-tests.md` | One xUnit test project mirroring the app; WebApplicationFactory against real PostgreSQL for data-access tests. | NUnit (viable alternative) |
| `adrs/api/role-based-authorization.md` | Role gates at the endpoint layer + ownership checks in services. Deny by default. Requires `jwt-bearer-auth`. | Policy engine (OPA/Casbin) at larger scale |
| `adrs/database/uuid-primary-keys.md` | All PKs are UUIDs. No auto-increment. | Auto-increment integers (simpler but less secure) |
| `adrs/database/snake-case-naming.md` | snake_case tables/columns via EF Core naming convention. | PascalCase with quoting (non-idiomatic for PostgreSQL) |
| `adrs/database/timestamptz-always.md` | All datetimes are timestamptz. C# uses DateTimeOffset. | timestamp without timezone (loses timezone context) |
| `adrs/deployment/container-per-process.md` | The app (API + SPA bundle, one process) and PostgreSQL as separate containers. | Several processes in one container (harder to scale, restart, and reason about) |
| `adrs/deployment/github-actions-ci.md` | CI gate on every push/PR: lint (enforcement configs), typecheck, tests, build. Deploys are separate workflows. | GitLab CI / Azure DevOps (if repos move) |
| `adrs/angular/standalone-components.md` | All components standalone. No NgModules. | — |
| `adrs/angular/signals-state.md` | Angular Signals for reactive state. RxJS only for HTTP/async. | RxJS BehaviorSubjects (more boilerplate) |
| `adrs/angular/tailwind-no-css.md` | Tailwind utility classes only. No component CSS files. | Component-scoped SCSS (if team prefers) |

## Optional (pick based on project needs)

<!-- generated from profiles/profiles.toml — edit the manifest and run scripts/generate_profiles.py -->
| ADR | Summary | When to Include |
|-----|---------|-------------|
| `adrs/api/offset-pagination.md` | Offset pagination with page/pageSize/sortBy/sortDir. Requires `rest-envelope`. | Any project with list endpoints |
| `adrs/database/soft-deletes.md` | Soft deletion via nullable `deleted_at` column. | Projects needing audit trails or undo capability |
| `adrs/angular/separate-template-file.md` | Component templates in separate `.html` files via `templateUrl`. No inline templates. | Team preference for HTML tooling |

---

## Key Cross-Cutting Concerns

When using this stack, these patterns emerge from the combination of ADRs:

- **One origin, no CORS:** the SPA is served from the API's `wwwroot` with an `index.html` fallback registered after API routes. Refresh cookies stay first-party; a CORS policy appearing anywhere signals drift.
- **EF migrations are the schema story:** no Flyway/DbUp at this rung — migrations are generated next to the change and applied with `dotnet ef database update` (dev) or on deploy.
- **Background-work loss semantics are explicit:** bounded `Channel<T>` + `BackgroundService` for tolerable-loss work; Hangfire on the app's PostgreSQL for must-survive work; a broker means graduating profiles, not adding one here.
- **Navigation properties are allowed** — there are no module boundaries yet. The discipline that must hold anyway: services behind interfaces, DTOs at the boundary, feature folders coherent. That is what makes graduation mechanical.
- **Naming translation:** C# PascalCase → snake_case columns (naming convention package) → camelCase JSON.
- **Deploy is compose on one box:** the app container (API + SPA) and postgres, `docker compose up -d` on the server. Scaling is vertical first, then N app containers behind any TCP load balancer — the app is stateless (sessions live in PostgreSQL/JWT).

## Development Workflow

### Local Development Commands

```bash
# infrastructure (postgres only — the app runs on the host)
docker compose up -d

# apply EF migrations, run the API (serves on http://localhost:5000)
dotnet ef database update --project src/MyApp.Web
dotnet run --project src/MyApp.Web

# frontend dev server (separate terminal; proxies /api to :5000)
cd client && npm install && npm start

# tests (real PostgreSQL, TEST_DATABASE_URL)
dotnet test
```

### Production Deployment

```bash
# on the server: build and start the app + postgres
docker compose -f docker-compose.prod.yml up -d --build

# apply migrations against the production database
dotnet ef database update --project src/MyApp.Web --connection "$DATABASE_URL"

# verify
curl https://myapp.example.com/health
```
