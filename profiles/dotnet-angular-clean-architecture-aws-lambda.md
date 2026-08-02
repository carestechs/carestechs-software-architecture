# Stack Profile: .NET + Angular Clean Architecture (AWS Lambda)

**Status:** Active
**Assumes:** .NET 10+, Angular 20+, PostgreSQL, EF Core 10+, Flyway, AWS Lambda, API Gateway, SQS, SAM/CloudFormation, Tailwind CSS 4+, Tauri 2 + Rust (desktop)

## Golden Skeleton

A buildable reference implementation lives at
[`skeletons/dotnet-angular-clean-architecture-aws-lambda/`](../skeletons/dotnet-angular-clean-architecture-aws-lambda/).
CI executes what can run without AWS (unit tests everywhere; xUnit against a Flyway-migrated
PostgreSQL and the real SQS provider against LocalStack) and lints what cannot
(`sam validate` / `cfn-lint` / `sam build` on the per-module templates). Its README carries
the honest proven-vs-linted table.

## Overview

A curated set of ADRs for building a distributed system of independently deployable modules, each following Clean Architecture (Domain/Application/Data/Api layers). Backend modules deploy as AWS Lambda functions behind API Gateway. Modules communicate asynchronously via SQS queues. The Angular SPA is deployed to S3 + CloudFront. Database schema is managed by Flyway SQL migrations, not EF Core migrations.

This profile differs from the Docker Compose modular monolith in three key ways: (1) each module is independently deployable, not co-hosted in a single API; (2) deployment targets AWS serverless infrastructure, not Docker containers; (3) business logic uses CQRS with command/query handlers and rich domain entities, not a service-layer pattern.

---

## Solution Structure

```
MyApp/
├── MyApp.slnx                                  # Solution file (slnx format)
│
├── Common.Lib/                                  # Core contracts & patterns
│   ├── Contracts/
│   │   ├── ICommand.cs                          # ICommand<TResult>
│   │   ├── ICommandHandler.cs                   # ICommandHandler<TCommand, TResult>
│   │   ├── IQuery.cs                            # IQuery<TResult>
│   │   ├── IQueryHandler.cs                     # IQueryHandler<TQuery, TResult>
│   │   ├── IEvent.cs                            # IEvent marker interface
│   │   ├── IEventBus.cs                         # In-process event publishing
│   │   ├── IReactors.cs                         # IReactors<TEvent> for side effects
│   │   ├── IUnitOfWork.cs                       # SaveChangesAsync abstraction
│   │   ├── IJsonSerializer.cs
│   │   ├── ISecretsProvider.cs
│   │   ├── IParametersProvider.cs
│   │   └── IQueueProvider.cs
│   ├── Results/
│   │   └── Result.cs                            # Result<T> with Error and ErrorType
│   ├── Errors/
│   │   └── Error.cs                             # Error record, GenericErrors factory
│   └── Common.Lib.csproj
│
├── Common.Core/                                 # Cross-module shared contracts
│   ├── Events/                                  # Events that cross module boundaries
│   │   ├── BatchImportStartedEvent.cs
│   │   └── MetadataReceivedEvent.cs
│   ├── Contracts/                               # Cross-module service interfaces
│   │   └── IImageProvider.cs
│   └── Common.Core.csproj
│
├── Common.Providers/                            # Infrastructure implementations
│   ├── Data/
│   │   └── EfUnitOfWork.cs                      # IUnitOfWork → DbContext.SaveChangesAsync
│   ├── Events/
│   │   └── EventBusProvider.cs                  # In-process IEventBus
│   ├── Queue/
│   │   ├── HttpQueueProvider.cs                 # Dev: polls local queue server
│   │   ├── SqsQueueProvider.cs                  # Prod: Amazon SQS
│   │   └── BatchJobQueueProvider.cs             # Prod: AWS Batch (compute-heavy jobs)
│   ├── Parameters/
│   │   ├── ParametersFileProvider.cs            # Dev: reads .parameters JSON file
│   │   └── ParametersSSMProvider.cs             # Prod: AWS SSM Parameter Store
│   ├── Secrets/
│   │   ├── SecretsFileProvider.cs               # Dev: reads .secrets JSON file
│   │   └── SecretsManagerProvider.cs            # Prod: AWS Secrets Manager
│   └── Common.Providers.csproj
│
├── Common.Database/                             # Flyway migration scripts
│   └── db/
│       ├── V1__Initial_Tables.sql
│       ├── V2__Add_Feature.sql
│       └── ...
│
├── Common.QueueServer/                          # Lightweight HTTP queue for local dev
│   └── Program.cs
│
├── FeatureA.Domain/                             # Domain layer (zero dependencies)
│   ├── Models/
│   │   ├── Entity1.cs                           # Rich entity: private set, Create(), methods
│   │   └── Entity2.cs
│   ├── Enums/
│   │   └── EntityStatus.cs
│   └── FeatureA.Domain.csproj
│
├── FeatureA.Application/                        # Application layer (CQRS)
│   ├── Contracts/
│   │   └── IEntity1Repository.cs                # Repository interfaces
│   ├── Models/
│   │   └── Entity1Context.cs                    # Response DTOs
│   ├── Commands/
│   │   ├── CreateEntity1Command.cs              # record : ICommand<Result<Guid>>
│   │   └── Handlers/
│   │       └── CreateEntity1CommandHandler.cs    # ICommandHandler implementation
│   ├── Queries/
│   │   ├── GetEntity1ByIdQuery.cs               # record : IQuery<Entity1Context?>
│   │   └── Handlers/
│   │       └── GetEntity1ByIdQueryHandler.cs     # IQueryHandler implementation
│   ├── Reactors/
│   │   └── SomeEventReactor.cs                  # IReactors<TEvent> implementation
│   └── FeatureA.Application.csproj              # Refs: Domain, Common.Lib, Common.Core
│
├── FeatureA.Data/                               # Data layer (EF Core)
│   ├── FeatureADbContext.cs                     # DbContext with lowercase naming loop
│   ├── Entity1Repository.cs                     # IEntity1Repository implementation
│   └── FeatureA.Data.csproj                     # Refs: Domain, Application, EF Core
│
├── FeatureA.Api/                                # API layer (composition root)
│   ├── Program.cs                               # DI, DbContext, handlers, endpoints
│   ├── Endpoints/
│   │   └── Entity1Endpoints.cs                  # Minimal API MapGroup endpoints
│   ├── Requests/
│   │   └── CreateEntity1Request.cs              # record request DTOs
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── deploy/
│   │   ├── build.py                             # dotnet publish + package
│   │   └── deploy.py                            # Upload to S3, update Lambda
│   ├── .secrets                                 # Dev credentials (gitignored)
│   ├── .parameters                              # Dev config (gitignored)
│   └── FeatureA.Api.csproj                      # Refs: Application, Data, Providers
│
├── FeatureA.Worker/                             # Worker (SQS → Lambda or background service)
│   ├── Function.cs                              # SQS Lambda handler (prod)
│   ├── Program.cs                               # BackgroundService host (dev)
│   └── FeatureA.Worker.csproj
│
├── FeatureA.BatchWorker/                        # Compute-heavy worker (AWS Batch)
│   ├── Program.cs                               # Dev: BackgroundService + HttpQueueProvider
│   │                                            # Prod: reads JOB_PAYLOAD env var, processes, exits
│   ├── docker/
│   │   └── dockerfile                           # Multi-stage: SDK publish → runtime image
│   └── FeatureA.BatchWorker.csproj
│
├── Maintenance.Application/                     # Maintenance routines (shared logic)
│   ├── Contracts/
│   │   └── IMaintenanceRoutine.cs               # Name, ExecuteAsync(options)
│   ├── Routines/
│   │   ├── ImportDataRoutine.cs                 # Example: bulk data import
│   │   └── VerifyPipelineRoutine.cs             # Example: end-to-end verification
│   └── Maintenance.Application.csproj
│
├── Maintenance.Worker/                          # Dual-mode maintenance CLI + scheduler
│   ├── Program.cs                               # CLI: list | run <name> [--dry-run] [--no-aws]
│   │                                            # Scheduler: default starts BackgroundService on timer
│   └── Maintenance.Worker.csproj
│
├── FeatureA.Infra/                              # CloudFormation/SAM template
│   ├── resources.yml                            # Lambda, API GW, SQS, IAM, SSM
│   ├── deploy/
│   │   ├── build.py
│   │   └── deploy.py                            # sam deploy
│   └── FeatureA.Infra.csproj
│
├── FeatureA.Web/                                # Angular SPA (if module has a UI)
│   ├── src/
│   │   ├── app/
│   │   │   ├── components/
│   │   │   ├── services/
│   │   │   ├── app.component.ts
│   │   │   ├── app.config.ts
│   │   │   └── app.routes.ts
│   │   ├── environments/
│   │   │   ├── environment.ts                   # Dev API URL
│   │   │   └── environment.prod.ts              # __API_BASE_URL__ placeholder
│   │   └── index.html
│   ├── angular.json
│   ├── package.json
│   └── deploy/
│       ├── build.py                             # ng build --configuration production
│       └── deploy.py                            # S3 upload + CloudFront invalidation
│
├── GUI.Desktop/                                 # Tauri 2 desktop app (Angular + Rust)
│   ├── GUI.Desktop.csproj                       # .NET wrapper (IsPackable: false)
│   ├── package.json                             # Node dependencies (Angular, Tauri CLI)
│   ├── angular.json                             # Angular workspace config
│   ├── vite.config.ts                           # Vite build config (dev server)
│   ├── tsconfig.json
│   ├── src/                                     # Angular frontend (desktop-specific)
│   │   ├── app/
│   │   │   ├── components/
│   │   │   ├── services/
│   │   │   │   └── api.ts                       # Calls remote APIs + Tauri invoke()
│   │   │   ├── app.component.ts
│   │   │   ├── app.config.ts
│   │   │   └── app.routes.ts
│   │   ├── environments/
│   │   └── index.html
│   └── src-tauri/                               # Rust/Tauri backend
│       ├── Cargo.toml                           # Rust deps (tauri, reqwest, serde, plugins)
│       ├── tauri.conf.json                      # App config (window size, dev URL, CSP)
│       ├── src/
│       │   ├── main.rs                          # Entry point + command registration
│       │   ├── lib.rs                           # Tauri app setup + managed state
│       │   ├── commands/                        # Thin IPC wrappers (grouped by domain)
│       │   ├── services/                        # Business logic + remote API proxying
│       │   └── error.rs                         # Unified AppError (thiserror + Serialize)
│       ├── capabilities/
│       │   └── default.json                     # Least-privilege permissions
│       └── icons/                               # Platform-specific icons
│
├── Common.Infra/                                # Shared infrastructure
│   ├── resources.yml                            # VPC, RDS, Route53, shared IAM
│   └── deploy/
│
├── build.py                                     # Root build orchestrator
├── deploy.py                                    # Root deploy orchestrator
│
└── Tests/
    ├── FeatureA.Domain.Tests/
    ├── FeatureA.Application.Tests/
    └── FeatureA.Api.Tests/
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
| `adrs/deployment/aws-lambda-serverless.md` | Each module's API deploys as its own Lambda behind API Gateway. | — |
| `adrs/deployment/aws-sam-infrastructure.md` | All infrastructure defined as SAM/CloudFormation templates. Stack-per-module. | `aws-lambda-serverless` |
| `adrs/deployment/aws-secrets-parameters.md` | Secrets Manager for credentials, SSM for config. File-based providers for dev. | `aws-lambda-serverless` |
| `adrs/deployment/flyway-migrations.md` | Hand-written SQL migrations. EF Core is runtime-only ORM. | — |

## Recommended (strong defaults — can be swapped with noted alternatives)

<!-- generated from profiles/profiles.toml — edit the manifest and run scripts/generate_profiles.py -->
| ADR | Summary | Alternative |
|-----|---------|-------------|
| `adrs/dotnet/xunit-per-module-tests.md` | xUnit test projects mirroring modules/layers. Real PostgreSQL (Testcontainers) for data-access tests. | NUnit (viable alternative) |
| `adrs/dotnet/structured-logging.md` | ILogger<T> with message templates. JSON output + correlation IDs in production. | Serilog as host provider (compatible) |
| `adrs/deployment/github-actions-ci.md` | CI gate on every push/PR: lint (enforcement configs), typecheck, tests, build. Deploys are separate workflows. | GitLab CI / Azure DevOps (if repos move) |
| `adrs/database/uuid-primary-keys.md` | All PKs are UUIDs. Generated server-side with `Guid.NewGuid()`. | Auto-increment integers (simpler but less secure) |
| `adrs/database/lowercase-naming.md` | Lowercase table/column names via `OnModelCreating` loop. | `snake-case-naming` with naming convention package |
| `adrs/database/timestamptz-always.md` | All datetimes are `timestamptz`. C# uses `DateTimeOffset`. | `timestamp` without timezone (loses context) |
| `adrs/api/jwt-bearer-auth.md` | JWT Bearer tokens. Short-lived access + rotated refresh. | Cognito / API Gateway JWT authorizer (managed alternative) |
| `adrs/deployment/queue-based-decoupling.md` | Cross-module async work via a durable queue behind `IQueueProvider` (SQS on AWS; RabbitMQ/Redis in compose deployments). | Direct synchronous calls between modules (tighter coupling, cascading failures) |
| `adrs/deployment/idempotent-queue-consumers.md` | At-least-once discipline: idempotent handlers keyed on event ID, DLQ on every queue, triage-fix-redrive. | None viable — an SQS pipeline without DLQs is an unmonitored outage |
| `adrs/deployment/correlation-propagation.md` | One correlation ID from ingress through every queue hop and log scope. | Managed tracing only (X-Ray/OTel — complementary, sampling-limited) |
| `adrs/deployment/aws-batch-workers.md` | Compute-heavy jobs run on AWS Batch (Fargate). Dual-mode Program.cs: BackgroundService polling in dev, single-shot JOB_PAYLOAD in prod. | Step Functions for orchestration, or Lambda with higher memory/timeout |
| `adrs/angular/standalone-components.md` | All components standalone. No NgModules. | — |
| `adrs/angular/signals-state.md` | Angular Signals for reactive state. RxJS only for HTTP/async. | RxJS BehaviorSubjects |
| `adrs/angular/tailwind-no-css.md` | Tailwind utility classes only. No component CSS files. | Component-scoped SCSS |

## Optional (pick based on project needs)

<!-- generated from profiles/profiles.toml — edit the manifest and run scripts/generate_profiles.py -->
| ADR | Summary | When to Include |
|-----|---------|-------------|
| `adrs/deployment/tauri-desktop-shell.md` | Tauri 2 desktop shell wrapping the Angular frontend. Rust backend for native OS access. | Modules needing a native desktop client (image inspection, offline access) |
| `adrs/deployment/maintenance-cli-scheduler.md` | Dual-mode maintenance worker: CLI for on-demand routines (`run <name> --dry-run`) and scheduler for periodic execution. | Projects with recurring data migration, pipeline verification, or cleanup tasks |
| `adrs/deployment/fifo-ordered-processing.md` | FIFO only for per-aggregate ordering; group ID = aggregate ID; explicit dedup IDs. | Workflows where out-of-order processing of one aggregate is a correctness bug |
| `adrs/deployment/eventbridge-domain-events.md` | Domain facts on the bus with versioned detail-types; directed work stays on queues. | Analytics/integration consumers the producer must not know about |
| `adrs/database/soft-deletes.md` | Soft deletion via `IsActive` flag or `DeletedAt` column. | Entities needing audit trails or undo |
| `adrs/api/rest-envelope.md` | All responses wrapped in `{ data, meta }` envelope. | Projects wanting a uniform response contract — required if `offset-pagination` is included |
| `adrs/api/offset-pagination.md` | Offset pagination with page/pageSize. Requires `rest-envelope`. | Any project with list endpoints |
| `adrs/api/role-based-authorization.md` | Role gates at the endpoint layer + ownership checks in services. Deny by default. Requires `jwt-bearer-auth`. | Policy engine (OPA/Casbin) at larger scale |
| `adrs/angular/separate-template-file.md` | Component templates in separate `.html` files. | Team preference for HTML tooling |

---

## Key Cross-Cutting Concerns

When using this stack, these patterns emerge from the combination of ADRs:

- **Naming translation:** C# PascalCase properties → lowercase database columns (via `OnModelCreating` loop) → camelCase JSON (via System.Text.Json default policy). No naming convention package needed.
- **Time handling:** Backend stores UTC `DateTimeOffset`, database uses `timestamptz`, frontend converts to local display time.
- **ID strategy:** UUIDs flow end-to-end: generated in C# with `Guid.NewGuid()`, stored as `uuid` in PostgreSQL, serialized as strings in JSON.
- **Module isolation:** Each module has four projects (Domain/Application/Data/Api) with its own DbContext, Lambda, and SAM stack. Cross-module communication is by ID + shared event via queue.
- **Dev/Prod parity:** The same `Program.cs` runs both locally (Kestrel + local PostgreSQL + file-based config) and in production (Lambda + RDS PostgreSQL + AWS SDK providers). Only the DI registrations differ based on `ASPNETCORE_ENVIRONMENT`.
- **Local database:** PostgreSQL runs locally as a single Docker container — infrastructure only. The application itself is never containerized in this profile (that is what `aws-lambda-serverless.md`'s conflicts with the Docker ADRs are about). Flyway applies the same `Common.Database/db/` migrations locally and in production, so the schema (uuid, timestamptz) is identical in both environments.
- **No secrets in code:** Development uses gitignored `.secrets`/`.parameters` files. Production reads from AWS Secrets Manager and SSM Parameter Store. Application code uses the abstraction layer only.
- **Migration strategy:** Flyway runs against the shared PostgreSQL database. All modules share one migration history. Each module's DbContext maps only its own tables.
- **Reliability floor:** every queue has a DLQ and an idempotent consumer keyed on event ID; failures park, get triaged, and are redriven after the fix. Correlation IDs minted at ingress ride every queue hop.
- **Queue abstraction:** `IQueueProvider` → `HttpQueueProvider` (dev, polls local `Common.QueueServer`) or `SqsQueueProvider` (prod) or `BatchJobQueueProvider` (prod, submits AWS Batch jobs with `JOB_PAYLOAD` env var). Reactors enqueue; workers dequeue.
- **Compute worker duality:** Batch workers have two execution paths in the same `Program.cs`. Development: `BackgroundService` polls local HTTP queue, processes jobs in a loop. Production: reads `JOB_PAYLOAD` env var, deserializes, runs the orchestrator, exits. No host startup in production — just a console app.
- **Maintenance operations:** Maintenance workers support CLI mode (`list`, `run <routine> --dry-run`) for ad-hoc execution and scheduler mode (default, runs routines on a timer). Routines are registered by name in DI and resolved dynamically.
- **Angular prod deployment:** `ng build --configuration production` with environment file replacement → deploy to S3 → inject API URL via `sed` replacement of `__API_BASE_URL__` → CloudFront cache invalidation.
- **Desktop app:** Tauri 2 provides a native shell with its own purpose-built Angular frontend (not a repackaged web app). Native OS operations and security-sensitive API calls (auth, secrets) go through Rust via `invoke()`. Public endpoints may use the Tauri HTTP plugin. Secrets stay in Rust managed state — never in the webview. Rust-to-frontend updates use Tauri events (`emit`/`listen`), not polling. Large binary data uses the asset protocol, not IPC JSON. `tauri build` produces platform-specific installers.

## Development Workflow

- **Local development first:** After creating the solution structure and wiring DI in `Program.cs`, the application must build, run, and be locally testable before adding feature code. Local dev uses SQLite, file-based config, and an HTTP queue server — no AWS credentials required.

### Local Development Commands

```bash
# Start local PostgreSQL (infrastructure container only - the app itself is never containerized)
docker run -d --name dev-postgres -e POSTGRES_PASSWORD=dev -p 5432:5432 postgres:16

# Apply Flyway migrations to the local database
flyway -url=jdbc:postgresql://localhost:5432/postgres -user=postgres -password=dev -locations=filesystem:Common.Database/db migrate

# Start the local queue server (for cross-module async events)
dotnet run --project Common.QueueServer

# Start a module's API (each module runs independently)
dotnet run --project FeatureA.Api

# Start a batch worker in dev mode (polls local HTTP queue)
dotnet run --project FeatureA.BatchWorker

# Start Angular dev server (if module has a web UI)
cd FeatureA.Web && ng serve

# List available maintenance routines
dotnet run --project Maintenance.Worker -- list

# Run a maintenance routine (dry run first, then for real)
dotnet run --project Maintenance.Worker -- run import-data --dry-run
dotnet run --project Maintenance.Worker -- run import-data

# Run tests
dotnet test

# Run all tests for a specific module
dotnet test Tests/FeatureA.Domain.Tests
```

### Production Deployment

```bash
# Build all modules (runs dotnet publish + package for each)
python build.py <project> production us-east-2

# Deploy infrastructure stacks (creates/updates Lambda, API GW, SQS, etc.)
python deploy.py <project> production us-east-2

# Run Flyway migrations against production database
flyway -url=jdbc:postgresql://host:5432/db -user=user -password=pass migrate

# Verify
curl https://<api-gw-url>/prod/health
```
