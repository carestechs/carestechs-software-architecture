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
| Cold starts, real Lambda invocation, CloudFront/S3 frontend delivery | **Not proven** — requires a real AWS account (the deferred phase-3 smoke workflow) |

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
```

## Deliberately not demonstrated (yet)

- **Real deployment** — `sam deploy`, IAM in anger, API Gateway wiring, cold starts: the
  phase-3 real-AWS smoke workflow, when a sandbox account exists.
- **The Angular SPA + S3/CloudFront delivery and the build/deploy orchestrators** — planned
  as the follow-up slice; the backend is the semantic core of this profile.
- **JWT auth** (Recommended tier) — proven three times in the sibling skeletons; elided here
  to keep the first AWS skeleton focused on what is new (queues, workers, SAM).
- **aws-batch-workers / maintenance-cli / tauri** — Optional-tier, out of skeleton scope.

This skeleton is held to the same constraints it demonstrates.
