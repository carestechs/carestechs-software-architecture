# Stack Profile: .NET Multi-Tenant Event-Driven Platform (AWS Lambda)

**Status:** Active
**Assumes:** .NET 10+, PostgreSQL (database per tenant), DynamoDB, EF Core 10+ (runtime-only), DbUp, AWS Lambda, API Gateway, SQS (Standard + FIFO), EventBridge, Cognito, IoT Core (MQTT), S3, SAM/CloudFormation

## Overview

A curated set of ADRs for building a multi-tenant, event-driven backend platform (contact-center, messaging, or workflow class) as a distributed system of independently deployable Clean Architecture modules on AWS. Tenants are isolated at the storage layer (database-per-tenant PostgreSQL + tenant-scoped DynamoDB partition keys). Identity is delegated to Cognito with Lambda-trigger customization. Real-time client updates ride managed MQTT. Integration with third-party platform APIs is quarantined in a Bridge module. Frontends are separate projects consuming the platform's APIs — this profile is backend-only.

This profile differs from `dotnet-angular-clean-architecture-aws-lambda` in five ways: (1) storage is multi-tenant — a database per tenant plus a DynamoDB hot path, not one shared PostgreSQL; (2) module boundaries are mechanically enforced — a PostgreSQL schema per module and one facade interface per module, not convention alone; (3) authentication is managed (Cognito + triggers), not a self-issued JWT stack; (4) the system is event-driven at its core — SQS pipelines, a transactional outbox, and one-Lambda-per-concern workers, not request/response with occasional queues; (5) no bundled frontend or desktop shell.

---

## Solution Structure

```
MyPlatform/
├── MyPlatform.slnx                              # Solution file (slnx format)
│
├── Common.Lib/                                  # Core contracts (ICommand, IQuery, IEvent, Result<T>, tenancy identifiers)
├── Common.Core/                                 # Cross-module events and shared interfaces
├── Common.Providers/                            # EfUnitOfWork, EventBus, Queue/Secrets/Parameters providers
├── Common.Database/                             # V{N}__{name}.sql DbUp scripts (embedded resources)
├── Common.DatabaseCli/                          # Operator console: apply migrations against a target env
├── Common.QueueServer/                          # Local dev HTTP queue server
├── Common.Workers/                              # Shared Lambda-entry helpers (SQS message extensions, polling base)
├── Common.Infra/                                # Shared infrastructure (VPC, RDS, Route53)
│
├── FeatureA.Domain/                             # Rich entities, value objects, enums (zero dependencies)
├── FeatureA.Application/                        # CQRS handlers, strategies, reactors, IFeatureAModuleApi facade
├── FeatureA.Data/                               # DbContext (HasDefaultSchema("featurea")) + DynamoDB repositories
├── FeatureA.Api/                                # Minimal API endpoints (Lambda behind API Gateway)
├── FeatureA.IngestWorker/                       # Lambda: one project per SQS-triggered concern
├── FeatureA.StatusWorker/                       # Lambda: one project per SQS-triggered concern
├── FeatureA.Infra/                              # SAM: API GW + queues + Lambdas + DDB tables + IAM
│
├── Tenancy.{Domain,Application,Data,Api,Infra}  # Organizations/workspaces; TenantDatabaseProvisioner (DbUp)
├── Tenancy.Cli/                                 # Operator CLI: org create/disable/enable/list
├── Identity.{Domain,Application,Data,Api,Infra} # Users (per-tenant PG), module facade consumed by others
│
├── Auth.Domain/                                 # Cognito trigger event contract types
├── Auth.Application/                            # Pre-token enrichment + post-auth lifecycle handlers
├── Auth.PreTokenGeneration/                     # Lambda: Cognito pre-token trigger (tenant + app claims)
├── Auth.PostAuthentication/                     # Lambda: Cognito post-auth trigger (activate, stamp last login)
├── Auth.Infra/                                  # SAM: trigger Lambdas + Cognito invoke permissions
│
├── Bridge.Domain/                               # Provider webhook DTOs, normalization types
├── Bridge.Application/                          # Provider client contract, outbound strategies, webhook normalizer
├── Bridge.Data/                                 # Provider API client, provider-state DDB caches, token provider
├── Bridge.Api/                                  # Webhook verify + health (thin HTTP entry)
├── Bridge.InboundWebhook/                       # Lambda: HMAC validate + normalize + fan-out
├── Bridge.OutboundMessage/                      # Lambda: consume outbound queue → provider API calls
├── Bridge.Infra/                                # SAM: Lambdas + API GW + DDB + queues + provider secret
│
├── Notification.Application/                    # Reactors producing push payloads
├── Notification.Worker/                         # Lambda: consume push queue → IoT MQTT publish
├── Notification.Authorizer/                     # Lambda: IoT custom authorizer (app JWT → per-user topic policy)
├── Notification.Infra/                          # SAM: push queue + Lambdas + IoT authorizer
│
├── Outbox.Dispatch/                             # Lambda: scheduled outbox drain → SQS
│
├── Maintenance.Application/                     # IMaintenanceRoutine implementations
├── Maintenance.Cli/                             # Operator console: list | run <name> [--dry-run]
├── Maintenance.Worker/                          # Lambda: direct-invoke { RoutineName, DryRun }
├── Maintenance.Infra/                           # SAM: worker Lambda + EventBridge rule per routine
│
├── build.py                                     # Root build orchestrator
├── deploy.py                                    # Root deploy orchestrator
│
└── Tests/
    ├── FeatureA.Domain.Tests/
    ├── FeatureA.Application.Tests/
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
| `adrs/dotnet/module-facade.md` | One public `I<Module>ModuleApi` facade per consumed module; snapshot records; everything else internal. | `clean-architecture-layers` |
| `adrs/database/database-per-tenant.md` | A PostgreSQL database per tenant; per-tenant unit-of-work factory; tenant ids from validated claims. | — |
| `adrs/database/schema-per-module.md` | Each module owns a PG schema; ORM default-schema per module; optional per-module DB roles. | — |
| `adrs/database/dynamodb-hot-path.md` | High-throughput document data in DynamoDB with tenant-scoped partition keys; PG stays relational truth. | — |
| `adrs/deployment/dbup-migrations.md` | Embedded `V{N}__` SQL scripts applied by DbUp — at tenant provisioning and via operator CLI. | — |
| `adrs/api/cognito-authentication.md` | Managed user pool, per-app clients, pre-token/post-auth triggers; app DB masters user lifecycle. | — |
| `adrs/deployment/aws-lambda-serverless.md` | Each module's API deploys as its own Lambda behind API Gateway. | — |
| `adrs/deployment/aws-sam-infrastructure.md` | All infrastructure defined as SAM/CloudFormation templates. Stack-per-module. | `aws-lambda-serverless` |
| `adrs/deployment/aws-secrets-parameters.md` | Secrets Manager for credentials, SSM for config. File-based providers for dev. | `aws-lambda-serverless` |
| `adrs/deployment/queue-based-decoupling.md` | Cross-module async work via a durable queue behind `IQueueProvider` (SQS on AWS; RabbitMQ/Redis in compose deployments). | — |
| `adrs/deployment/idempotent-queue-consumers.md` | At-least-once discipline: idempotent handlers keyed on event ID, DLQ on every queue, triage-fix-redrive. | `queue-based-decoupling` |

## Recommended (strong defaults — can be swapped with noted alternatives)

<!-- generated from profiles/profiles.toml — edit the manifest and run scripts/generate_profiles.py -->
| ADR | Summary | Alternative |
|-----|---------|-------------|
| `adrs/database/transactional-outbox.md` | Correctness-critical events written in-transaction, drained by a scheduled dispatcher; latency-critical hints may bypass with a reconciliation path. | Direct enqueue everywhere (accepts lost events on crash) |
| `adrs/api/external-provider-bridge.md` | Third-party platform API isolated in one Bridge module; HMAC-validated webhooks normalized at the edge. | Only for platforms without external provider integration |
| `adrs/deployment/iot-mqtt-push.md` | Real-time client push via IoT Core MQTT; per-user topics; custom authorizer scopes policy to caller's subtree. | Polling (degraded), API GW WebSockets (self-managed fan-out) |
| `adrs/api/machine-to-machine-auth.md` | Opaque hashed API tokens for integration clients; dedicated authorizer injects tenant + scope. | OAuth client-credentials via Cognito (OIDC partners) |
| `adrs/deployment/s3-object-storage.md` | Tenant-scoped object keys, presigned transfer, metadata mastered in the DB. | Only for platforms without binary content |
| `adrs/deployment/correlation-propagation.md` | One correlation ID from ingress through every queue hop, log scope, outbox row, and push payload. | Managed tracing only (X-Ray/OTel — complementary, sampling-limited) |
| `adrs/api/role-based-authorization.md` | Role gates at the endpoint layer + ownership checks next to the data. Deny by default. | Policy engine (OPA/Casbin) at larger scale |
| `adrs/dotnet/xunit-per-module-tests.md` | xUnit test projects mirroring modules/layers. Real PostgreSQL for data-access tests. | NUnit (viable alternative) |
| `adrs/dotnet/structured-logging.md` | ILogger<T> with message templates. JSON output + correlation IDs in production. | Serilog as host provider (compatible) |
| `adrs/deployment/github-actions-ci.md` | CI gate on every push/PR: lint (enforcement configs), typecheck, tests, build. Deploys are separate workflows. | GitLab CI / Azure DevOps (if repos move) |
| `adrs/database/uuid-primary-keys.md` | All PKs are UUIDs, generated server-side. | Auto-increment integers (simpler but less secure) |
| `adrs/database/lowercase-naming.md` | Lowercase table/column names via `OnModelCreating` loop. | `snake-case-naming` with naming convention package |
| `adrs/database/timestamptz-always.md` | All datetimes are `timestamptz`. C# uses `DateTimeOffset`. | `timestamp` without timezone (loses context) |
| `adrs/api/rest-envelope.md` | All 2xx responses wrapped in `{ data, meta }` envelope. | Bare payloads (loses uniform meta) |
| `adrs/deployment/maintenance-cli-scheduler.md` | Maintenance routines runnable via operator CLI and scheduled worker (EventBridge rule per routine). | Ad-hoc scripts (unauditable) |

## Optional (pick based on project needs)

<!-- generated from profiles/profiles.toml — edit the manifest and run scripts/generate_profiles.py -->
| ADR | Summary | When to Include |
|-----|---------|-------------|
| `adrs/api/offset-pagination.md` | Offset pagination with page/pageSize. Requires `rest-envelope`. | Any project with list endpoints |
| `adrs/database/soft-deletes.md` | Soft deletion via `IsActive` flag or `DeletedAt` column. | Entities needing audit trails or undo |
| `adrs/deployment/aws-batch-workers.md` | Compute-heavy jobs on AWS Batch (Fargate) with dual-mode Program.cs. | Media transcoding, bulk imports beyond Lambda limits |
| `adrs/deployment/fifo-ordered-processing.md` | FIFO only for per-aggregate ordering; group ID = aggregate ID; explicit dedup IDs. | Workflows where out-of-order processing of one aggregate is a correctness bug |
| `adrs/deployment/eventbridge-domain-events.md` | Domain facts on the bus with versioned detail-types; directed work stays on queues. | Analytics/integration consumers the producer must not know about |
| `adrs/dotnet/strategy-dispatch.md` | One strategy class per content-type matrix cell; registry dispatch; mandatory unknown-kind fallback. | Dispatch matrices (content x session kinds) fed by external providers |

---

## Key Cross-Cutting Concerns

When using this stack, these patterns emerge from the combination of ADRs:

- **Tenant resolution flow:** Cognito pre-token trigger stamps tenant identifiers into the JWT → API Gateway authorizer validates → handlers read validated claims → per-tenant unit-of-work factory opens the tenant's database. Queue messages carry the same identifiers, stamped by the producer; workers open tenant scope from message metadata.
- **One Lambda per concern:** every SQS/Cognito/EventBridge/IoT entry point is its own project with its own deploy artifact, IAM scope, and log group. No multi-function worker projects — they couple deploys and blur IAM.
- **Reliability floor:** every queue has a DLQ and an idempotent consumer; failures park, get triaged, and are redriven after the fix. Correlation IDs minted at ingress ride every hop, so a parked message is traceable to the user action that produced it.
- **Two event tiers:** correctness-critical events ride the transactional outbox (at-least-once, in-transaction); latency-critical notifications enqueue directly and rely on REST reconciliation. Pick per event type, never both.
- **Module boundary enforcement stack:** cross-module-by-id (data model) + schema-per-module (storage) + module-facade (code) reinforce each other — a boundary violation has to defeat all three to ship.
- **Naming translation:** C# PascalCase properties → lowercase database columns → camelCase JSON. DynamoDB table names are plain constants in owning repositories.
- **Resource naming:** `{platform}-{module}-{role}` for Lambdas and queues (e.g., `myplatform-messaging-api`, `myplatform-outbox-dispatcher`). Environment separation is at the account/stack level, not name suffixes in runtime code.
- **Dev/prod parity:** the same code runs locally (Kestrel + local PostgreSQL + file-based secrets/parameters + HTTP queue server) and in production (Lambda + RDS + AWS providers). Only DI registrations differ by environment.
- **No secrets in code:** gitignored `.secrets`/`.parameters` files in dev; Secrets Manager + SSM in production, always behind the provider abstractions. Internal AWS endpoints (IoT data endpoint, etc.) are discovered at deploy time and passed as stack parameters.

## Development Workflow

### Local Development Commands

```bash
# Start local PostgreSQL (infrastructure container only — the app itself is never containerized)
docker run -d --name dev-postgres -e POSTGRES_PASSWORD=dev -p 5432:5432 postgres:16

# Apply DbUp migrations to a local tenant database
dotnet run --project Common.DatabaseCli -- --env local --tenant dev-org/dev-workspace

# Start the local queue server (cross-module async events)
dotnet run --project Common.QueueServer

# Start a module's API (each module runs independently)
dotnet run --project FeatureA.Api

# Run an SQS worker's handler loop locally (polls the local queue server)
dotnet run --project FeatureA.IngestWorker

# Operator CLIs
dotnet run --project Tenancy.Cli -- list
dotnet run --project Maintenance.Cli -- run close-stale --dry-run

# Run tests
dotnet test
```

### Production Deployment

```bash
# Build all modules (dotnet publish + package per Lambda project)
python build.py <module> production us-east-2

# Deploy infrastructure stacks (Lambda, API GW, SQS, DDB, IoT, Cognito triggers)
python deploy.py <module> production us-east-2

# Provision a tenant (creates the tenant database and replays full DbUp history)
dotnet run --project Tenancy.Cli -- org create <name>

# Verify
curl https://<api-gw-url>/prod/health
```
