# Stack Profile: .NET Event-Driven Modular System (Docker Compose)

**Status:** Active
**Assumes:** .NET 10+, PostgreSQL, EF Core 10+, Flyway, RabbitMQ, Angular 20+, Docker Compose v2, one VPS or a small VM fleet

## Golden Skeleton

A buildable reference implementation lives at
[`skeletons/dotnet-angular-event-driven-docker-compose/`](../skeletons/dotnet-angular-event-driven-docker-compose/).
CI executes this profile's claims against the production engines themselves — the same
PostgreSQL and RabbitMQ containers a laptop runs: the Flyway-migrated schema-per-module
storage, the RabbitMQ provider with its DLX retry topology declared in code, the
per-concern consumer (manual acks, explicit prefetch, poison parking observed in the DLQ),
and both halves of the transactional outbox. No emulator tier exists on this substrate;
its README carries the proven-vs-not table.

## Overview

The intermediate rung between the modular monolith and the serverless profiles: the lambda siblings' architecture on a substrate you own entirely. Modules follow the same Clean Architecture grammar as `dotnet-angular-clean-architecture-aws-lambda` — `<Module>.Domain/Application/Data/Api` projects, CQRS handlers, rich entities, reactors, module facades — and the async plane is first-class: a real broker, one always-on worker container per queue-triggered concern, and an optional transactional outbox. Nothing requires a cloud account: RabbitMQ stands where SQS/EventBridge stand, worker containers where Lambdas, nginx where API Gateway, env files where Secrets Manager.

The HTTP plane deliberately stays a single deployable: one thin API host container references every `<Module>.Api`, because on a VPS splitting the request path buys routing complexity and no isolation worth having. The async plane is where distribution pays — each worker scales, deploys, restarts, and fails independently.

This profile differs from `dotnet-angular-modular-monolith-docker-compose` (rungs 2-3) in three ways: (1) layers are projects, so the dependency rule is compiler-enforced instead of folder convention; (2) CQRS handlers, rich domain entities, and the Result pattern replace the service layer; (3) the async plane is first-class — a real broker with dead-letter topology and one worker container per concern, not a background queue bolted onto the monolith.

It differs from `dotnet-angular-clean-architecture-aws-lambda` (rung 4) in substrate only. The module grammar, facades, and queue abstraction are identical, which makes promotion mechanical: swap the RabbitMQ provider for the SQS provider, re-home each worker's handler in a Lambda entry, and put API Gateway in front — application code does not change. Choose this rung for steady or predictable load (always-on beats per-invocation pricing), latency-sensitive paths (no cold starts), vendor independence, data sovereignty, or on-prem constraints. Leave for rung 4/5 when traffic is bursty enough that scale-to-zero wins, or when you want the queues, identity, and patching to be someone else's job.

---

## Solution Structure

```
MyApp/
├── MyApp.slnx                                   # Solution file (slnx format)
├── Dockerfile                                   # One runtime image; api and workers override the command
├── docker-compose.yml                           # Dev: PostgreSQL + RabbitMQ (infrastructure only)
├── docker-compose.prod.yml                      # Prod: api + one service per worker + frontend + flyway job
├── .env.example                                 # Dev environment variable template
├── .env.production.example                      # Prod environment variable template
│
├── Common.Lib/                                  # Core contracts (ICommand, IQuery, IEvent, Result<T>)
├── Common.Core/                                 # Cross-module events and shared value types
├── Common.Providers/                            # EfUnitOfWork, EventBus, RabbitMqQueueProvider, topology declaration
├── Common.Database/                             # V{N}__{name}.sql Flyway migrations
│
├── App.Api/                                     # Thin API host container (composition root; references all <Module>.Api)
│
├── Catalog.Domain/                              # Rich entities, value objects, enums (zero dependencies)
├── Catalog.Application/                         # CQRS handlers, reactors, ICatalogModuleApi facade
├── Catalog.Data/                                # DbContext (HasDefaultSchema("catalog")) + repositories
├── Catalog.Api/                                 # Minimal API endpoint registrations (hosted by App.Api)
├── Catalog.IndexWorker/                         # Worker container: one project per queue-triggered concern
│
├── Identity.{Domain,Application,Data,Api}       # Users, tokens; IIdentityModuleApi facade
│
├── Notification.Application/                    # Reactors producing notification payloads
├── Notification.Worker/                         # Worker container: consume notification queue → deliver
│
├── Outbox.Dispatch/                             # Worker container: interval drain of the outbox table → RabbitMQ
│
├── client/                                      # Angular SPA → nginx container (proxies /api to App.Api)
└── Tests/
    ├── Catalog.Domain.Tests/
    ├── Catalog.Application.Tests/
    └── ...
```

---

## Required (core to this stack — do not omit)

These ADRs define the fundamental architecture. Removing any of them breaks the coherence of the stack.

<!-- generated from profiles/profiles.toml — edit the manifest and run scripts/generate_profiles.py -->
| ADR | Summary | Depends On |
|-----|---------|-------------|
| `adrs/dotnet/clean-architecture-layers.md` | Four projects per module: Domain, Application, Data, Api. Dependencies flow inward. | — |
| `adrs/dotnet/cqrs-handlers.md` | Commands and Queries with dedicated Handler classes. No service layer. No MediatR. | `clean-architecture-layers` |
| `adrs/dotnet/rich-domain-entities.md` | Private setters, static `Create()` factory, business logic in entities. | `clean-architecture-layers` |
| `adrs/dotnet/result-pattern-errors.md` | `Result<T>` return types from command handlers. Exceptions only for unexpected failures. | `cqrs-handlers` |
| `adrs/dotnet/event-driven-reactors.md` | In-process EventBus + Reactors for side effects. Cross-module work goes through queues. | `cqrs-handlers` |
| `adrs/dotnet/dto-at-boundary.md` | Never expose EF entities via API. DTOs (Context models) at the boundary. Mapping in handlers. | `cqrs-handlers` |
| `adrs/dotnet/async-all-the-way.md` | All I/O uses async/await. EF Core async methods throughout. | — |
| `adrs/dotnet/cross-module-by-id.md` | Modules reference each other by `Guid` ID only. No cross-module navigation properties. | `clean-architecture-layers` |
| `adrs/dotnet/thin-api-host.md` | One thin API host container: `Program.cs` composition root referencing every `<Module>.Api`; no logic of its own. | `clean-architecture-layers` |
| `adrs/deployment/queue-based-decoupling.md` | Cross-module async work via a durable queue behind `IQueueProvider` (SQS on AWS; RabbitMQ/Redis in compose deployments). | — |
| `adrs/deployment/rabbitmq-broker.md` | RabbitMQ as the production broker behind the queue abstraction: direct exchanges for work queues, topic exchanges for domain-event fan-out, DLX-based retry and dead-letter topology declared in code. | `queue-based-decoupling` |
| `adrs/deployment/idempotent-queue-consumers.md` | At-least-once discipline: idempotent handlers keyed on event ID, DLQ on every queue, triage-fix-redrive. | `queue-based-decoupling` |
| `adrs/deployment/container-per-process.md` | API host, each worker, and the frontend as separate compose services; api and workers share one runtime image with different commands. | `docker-multi-stage-builds` |
| `adrs/deployment/docker-multi-stage-builds.md` | All components packaged as Docker images with multi-stage builds. `dotnet/aspnet` final stage for backend. | — |
| `adrs/deployment/local-dev-compose.md` | `docker-compose.yml` for local infra, `docker-compose.prod.yml` for app services on shared network. | `docker-multi-stage-builds`, `env-connection-urls` |
| `adrs/deployment/env-connection-urls.md` | All config via env vars. External services via connection URLs. Strongly-typed `IConfiguration` sections validate at startup. | — |
| `adrs/deployment/flyway-migrations.md` | Hand-written SQL migrations. EF Core is runtime-only ORM. | — |

## Recommended (strong defaults — can be swapped with noted alternatives)

<!-- generated from profiles/profiles.toml — edit the manifest and run scripts/generate_profiles.py -->
| ADR | Summary | Alternative |
|-----|---------|-------------|
| `adrs/dotnet/module-facade.md` | One public `I<Module>ModuleApi` facade per consumed module; snapshot records; everything else internal. | Per-purpose contract interfaces in the shared contracts project (multiplies per consumer) |
| `adrs/database/transactional-outbox.md` | Correctness-critical events written in-transaction, drained by a scheduled dispatcher; latency-critical hints may bypass with a reconciliation path. | Direct enqueue everywhere (accepts lost events on crash) |
| `adrs/database/schema-per-module.md` | Each module owns a PG schema; ORM default-schema per module; optional per-module DB roles. | Single shared schema with review-enforced boundaries (Pattern B — drifts under team growth) |
| `adrs/deployment/correlation-propagation.md` | One correlation ID from ingress through every queue hop and log scope. | Managed tracing only (X-Ray/OTel — complementary, sampling-limited) |
| `adrs/dotnet/xunit-per-module-tests.md` | xUnit test projects mirroring modules/layers. Real PostgreSQL (Testcontainers) for data-access tests. | NUnit (viable alternative) |
| `adrs/dotnet/structured-logging.md` | ILogger<T> with message templates. JSON output + correlation IDs in production. | Serilog as host provider (compatible) |
| `adrs/deployment/github-actions-ci.md` | CI gate on every push/PR: lint (enforcement configs), typecheck, tests, build. Deploys are separate workflows. | GitLab CI / Azure DevOps (if repos move) |
| `adrs/database/uuid-primary-keys.md` | All PKs are UUIDs. Generated server-side with `Guid.NewGuid()`. | Auto-increment integers (simpler but less secure) |
| `adrs/database/lowercase-naming.md` | Lowercase table/column names via `OnModelCreating` loop. | `snake-case-naming` with naming convention package |
| `adrs/database/timestamptz-always.md` | All datetimes are `timestamptz`. C# uses `DateTimeOffset`. | `timestamp` without timezone (loses context) |
| `adrs/api/jwt-bearer-auth.md` | JWT Bearer tokens. Short-lived access + rotated refresh. | Keycloak or another self-hosted IdP (heavier; the managed equivalent is the lambda siblings' Cognito) |
| `adrs/deployment/nginx-spa-proxy.md` | Nginx serves built SPA and reverse-proxies `/api/` to backend. `try_files` for client-side routing. | Serving SPA from backend framework or separate CDN |
| `adrs/angular/standalone-components.md` | All components standalone. No NgModules. | — |
| `adrs/angular/signals-state.md` | Angular Signals for reactive state. RxJS only for HTTP/async. | RxJS BehaviorSubjects (more boilerplate) |
| `adrs/angular/tailwind-no-css.md` | Tailwind utility classes only. No component CSS files. | Component-scoped SCSS (if team prefers) |

## Optional (pick based on project needs)

<!-- generated from profiles/profiles.toml — edit the manifest and run scripts/generate_profiles.py -->
| ADR | Summary | When to Include |
|-----|---------|-------------|
| `adrs/api/rest-envelope.md` | All responses wrapped in `{ data, meta }` envelope. | Projects wanting a uniform response contract — required if `offset-pagination` is included |
| `adrs/api/offset-pagination.md` | Offset pagination with page/pageSize. Requires `rest-envelope`. | Any project with list endpoints |
| `adrs/api/role-based-authorization.md` | Role gates at the endpoint layer + ownership checks in services. Deny by default. Requires `jwt-bearer-auth`. | Policy engine (OPA/Casbin) at larger scale |
| `adrs/database/soft-deletes.md` | Soft deletion via `IsActive` flag or `DeletedAt` column. | Entities needing audit trails or undo |
| `adrs/deployment/fifo-ordered-processing.md` | FIFO only for per-aggregate ordering; group ID = aggregate ID; explicit dedup IDs. | Workflows where out-of-order processing of one aggregate is a correctness bug |
| `adrs/angular/separate-template-file.md` | Component templates in separate `.html` files via `templateUrl`. No inline templates. | Team preference for HTML tooling |
| `adrs/dotnet/strategy-dispatch.md` | One strategy class per content-type matrix cell; registry dispatch; mandatory unknown-kind fallback. | Dispatch matrices (content x session kinds) fed by external providers |

---

## Key Conventions

- **Dev/prod parity is trivial here:** production runs the same containers the laptop runs. There is no substitution ladder — PostgreSQL and RabbitMQ in the dev compose file ARE the production engines, pinned to the same versions.
- **Queue abstraction:** `IQueueProvider` → `RabbitMqQueueProvider` in every environment. Handlers never see the broker; topology (exchanges, queues, DLX retry wiring) is declared idempotently at startup from versioned config.
- **One worker container per concern:** each queue-triggered concern is its own console project and its own compose service, mirroring the lambda siblings' one-Lambda-per-concern. Workers reuse the API image with a different command.
- **Reliable publish:** with the transactional outbox selected, handlers write the outbox row in the business transaction; `Outbox.Dispatch` drains it to RabbitMQ on an interval.
- **Correlation:** one correlation ID from HTTP ingress through message headers, log scopes, and worker processing.
- **Auth is self-issued:** JWT bearer tokens from the Identity module — no managed identity service on this rung.
- **Database:** one PostgreSQL instance, schema per module, Flyway migrations applied by a one-shot compose job before the api and workers start.

## Local Development

```bash
# Start infrastructure (PostgreSQL + RabbitMQ; management UI on http://localhost:15672)
docker compose up -d

# Apply Flyway migrations
flyway -url=jdbc:postgresql://localhost:5432/postgres -user=postgres -password=dev -locations=filesystem:Common.Database/db migrate

# Run the API host (Kestrel, no container — fast inner loop)
dotnet run --project App.Api

# Run any worker the feature under work needs (plain console apps locally)
dotnet run --project Catalog.IndexWorker

# Angular dev server
cd client && npm start

# Build the production images
docker compose -f docker-compose.prod.yml build
```
