# Golden Skeleton: dotnet-angular-clean-architecture-aws-lambda

A minimal, **buildable** instance of `profiles/dotnet-angular-clean-architecture-aws-lambda.md`.
Its job is to prove, in CI, that the profile's ADRs compose — with an honest boundary: some
of this profile lives in AWS, and CI can only emulate part of it. The table below says
exactly which is which.

## Proven vs linted-only

| Claim | How it is proven |
|-------|------------------|
| Clean Architecture layers, CQRS handlers, rich entities, `Result<T>` flow, reactors | **Executed** — pure unit tests (run everywhere, no infrastructure) |
| Repositories + EF-runtime-only mapping against the Flyway-managed schema | **Executed in CI** — xUnit against PostgreSQL migrated by the real Flyway container |
| Queue pipeline: reactor → `IQueueProvider` → SQS, correlation attribute round-trip | **Executed in CI** — the production `SqsQueueProvider` against ElasticMQ (SQS-compatible) |
| Worker semantics: idempotent redelivery, transient-vs-permanent failure split | **Executed in CI** — the worker's real processing core, double-delivery asserted |
| Same code runs as Kestrel and Lambda (`AddAWSLambdaHosting`) | **Executed** for the Kestrel path (WebApplicationFactory); **packaging linted** via `sam build` |
| SAM stacks: per-module templates, queue + DLQ + redrive, `ReportBatchItemFailures` | **Linted only** — `sam validate --lint`, `cfn-lint`, `sam build`. NOT deployed: IAM, event-source wiring, and API Gateway integration are unproven here |
| Angular production build with environment file replacement; the `__API_BASE_URL__` placeholder survives into the bundle | **Executed in CI** — build + grep assertion |
| S3 upload, placeholder injection, CloudFront invalidation, `sam deploy` orchestration | **Scripted, not proven** — `Web/deploy/deploy.py`, root `build.py`/`deploy.py`; require AWS credentials |
| Cold starts, real Lambda invocation, IAM in anger | **Not proven** — the phase-3 workflow exists dormant (`.github/workflows/aws-smoke.yml`); activation is a one-time sandbox setup (`docs/phase3-aws-smoke.md`) |

## What it demonstrates

| Area | ADRs exercised |
|------|----------------|
| Four projects per module (`Catalog.{Domain,Application,Data,Api}`, `Orders.{...}` + `Orders.Worker`), dependencies flowing inward | `dotnet/clean-architecture-layers` |
| Commands/queries with dedicated handlers, no service layer, no MediatR; query handlers return nullable DTOs mapped to 404 at the edge | `dotnet/cqrs-handlers` |
| `Product.Create()` / `Order.Create()` factories with private setters; `Order.Confirm()` idempotent by design | `dotnet/rich-domain-entities` |
| `Result<T>` + `Error`/`GenericErrors` from handlers, mapped to Problem Details at the Api edge; exceptions only for the unexpected | `dotnet/result-pattern-errors` |
| In-process `EventBus` + `OrderPlacedReactor`; reactor failures isolated; cross-module work leaves through a queue | `dotnet/event-driven-reactors`, `deployment/queue-based-decoupling` |
| `OrderPlacedMessage` carries ids only; `orders.productid` has no FK | `dotnet/cross-module-by-id` |
| Hand-written `V{N}__` SQL applied by Flyway; EF Core runtime-only; lowercase identifiers via the `OnModelCreating` loop | `deployment/flyway-migrations`, `adrs/database/lowercase-naming` |
| Correlation id minted at API ingress, carried as an SQS message attribute, restored to the worker's log scope, orphan-marked when absent | `deployment/correlation-propagation` |
| Queue + DLQ + `maxReceiveCount: 3` in the template; partial-batch failures in the worker; permanent-vs-transient failure split | `deployment/idempotent-queue-consumers` |
| Dev/prod provider split: file secrets/parameters + local HTTP queue server in Development; SSM/Secrets Manager/SQS otherwise | `deployment/aws-secrets-parameters` |
| Stack-per-module SAM templates | `deployment/aws-sam-infrastructure`, `deployment/aws-lambda-serverless` |
| BannedApiAnalyzers as errors — the 0-warning build includes the Lambda projects | enforcement layer |

## The Web frontend

`Web/` is the Angular 20 SPA (standalone components, signals, Tailwind v4) consuming the
bare-DTO API (`GET /v1/products` — no envelope; this profile deliberately does not adopt
`rest-envelope`). Dev: `npm start` proxies `/v1` to `Catalog.Api` on :5000. Production:
`ng build` bakes the `__API_BASE_URL__` placeholder into the bundle; `Web/deploy/deploy.py`
injects the real URL, syncs to S3, and invalidates CloudFront — the profile's delivery
model. Root `build.py`/`deploy.py` orchestrate all module stacks plus the Web bundle.

## Run it locally

```bash
# infrastructure (postgres only)
docker compose -f - up -d <<'YAML'
services:
  postgres:
    image: postgres:16
    environment: { POSTGRES_PASSWORD: postgres, POSTGRES_DB: app }
    ports: ["5432:5432"]
YAML

# apply Flyway migrations
docker run --rm --network host -v $PWD/Common.Database/db:/flyway/sql \
  flyway/flyway:10 -url=jdbc:postgresql://localhost:5432/app \
  -user=postgres -password=postgres migrate

# the local queue server (stands in for SQS)
dotnet run --project Common.QueueServer

# each module runs independently (separate terminals)
dotnet run --project Catalog.Api   # http://localhost:5000-range
dotnet run --project Orders.Api
dotnet run --project Orders.Worker # dev mode: polls the local queue server

# tests: pure domain/handler tests always run; integration tests skip unless
# TEST_DATABASE_URL / AWS_ENDPOINT_URL are set (CI sets both)
dotnet test

# frontend dev server (separate terminal; proxies /v1 to Catalog.Api on :5000)
cd Web && npm install && npm start
```

## Deliberately not demonstrated (yet)

- **Real deployment** — `sam deploy`, IAM in anger, API Gateway wiring, cold starts: the
  phase-3 real-AWS smoke workflow, when a sandbox account exists.
- **JWT auth** (Recommended tier) — proven three times in the sibling skeletons; elided here
  to keep the first AWS skeleton focused on what is new (queues, workers, SAM).
- **aws-batch-workers / maintenance-cli / tauri** — Optional-tier, out of skeleton scope.

This skeleton is held to the same constraints it demonstrates.
