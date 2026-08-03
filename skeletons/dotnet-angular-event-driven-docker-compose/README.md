# Golden Skeleton: dotnet-angular-event-driven-docker-compose

A minimal, **buildable** instance of `profiles/dotnet-angular-event-driven-docker-compose.md`.
Its job is to prove, in CI, that the profile's ADRs compose. This is the cheapest skeleton
in the catalog to prove honestly: the substrate is containers you own, so CI runs the
**production engines themselves** — the same PostgreSQL and RabbitMQ a laptop and a VPS run.
There is no emulator tier and no stand-in tier.

## Proven vs not

| Claim | How it is proven |
|-------|------------------|
| Clean Architecture layers, CQRS handlers, rich entities, `Result<T>` flow, reactors | **Executed** — pure unit tests (run everywhere, no infrastructure) |
| Repositories + EF-runtime-only mapping against the Flyway-managed, schema-per-module storage (`catalog.*`, `orders.*`) | **Executed in CI** — xUnit against PostgreSQL migrated by the real Flyway container |
| Thin API host composing both modules; per-module unit-of-work wiring | **Executed in CI** — WebApplicationFactory over `App.Api` |
| RabbitMQ provider: durable publish, correlation header, topology-as-code (work/retry/DLQ declared on the way) | **Executed in CI** — the production provider against a real broker |
| Consumer semantics: manual acks, explicit prefetch, idempotent redelivery | **Executed in CI** — the real `OrderPlacedConsumer` confirms a real order end to end |
| DLX retry cycle and poison parking: nack → TTL retry → back; budget spent → DLQ | **Executed in CI** — a poison message is observed landing in `<queue>.dlq` |
| Transactional outbox: the row commits with the order; the drain publishes and stamps | **Executed in CI** — both halves, against PostgreSQL + RabbitMQ |
| Angular production build; nginx same-origin proxy | **Build executed in CI**; the nginx config is exercised only by the image build |
| Prod compose topology: one image, per-worker services, external infra network | **Images built in CI**; the full compose stack is not run end to end there — run it locally (below) |

The one thing CI does not do is `docker compose -f docker-compose.prod.yml up` and drive the
whole chain across containers. Every link in that chain is executed individually; the
composition is a local command away.

## What it demonstrates

| Area | ADRs exercised |
|------|----------------|
| Four projects per module (`Catalog.{Domain,Application,Data,Api}`, `Orders.{...}` + `Orders.Worker`), dependencies flowing inward | `dotnet/clean-architecture-layers` |
| One thin host (`App.Api`) referencing every `<Module>.Api`; modules self-register; NO shared `IUnitOfWork` — each module's handlers get one bound to their own DbContext | `dotnet/thin-api-host` |
| Commands/queries with dedicated handlers, no service layer, no MediatR | `dotnet/cqrs-handlers` |
| `Product.Create()` / `Order.Create()` factories; `Order.Confirm()` idempotent by design | `dotnet/rich-domain-entities` |
| `Result<T>` + Problem Details at the edge | `dotnet/result-pattern-errors` |
| In-process `EventBus` + `OrderPlacedReactor`; the reactor *records* (outbox), the dispatcher *publishes*, the worker *consumes* | `dotnet/event-driven-reactors`, `database/transactional-outbox`, `deployment/queue-based-decoupling` |
| RabbitMQ behind `IQueueProvider` in every environment; direct exchanges, DLX retry topology declared idempotently in code; manual acks; explicit prefetch; pinned image + volume | `deployment/rabbitmq-broker` |
| One always-on consumer project per queue-triggered concern; api and workers share one image with different commands | `deployment/container-per-process` |
| Hand-written `V{N}__` SQL; EF Core runtime-only; lowercase identifiers; `CREATE SCHEMA catalog/orders` | `deployment/flyway-migrations`, `database/lowercase-naming`, `database/schema-per-module` |
| Correlation id minted at API ingress, carried in the outbox row and the AMQP header, restored to the worker's log scope, orphan-marked when absent | `deployment/correlation-propagation` |
| Redelivery processed once (idempotent confirm), transient-vs-permanent failure split, DLQ on every work queue | `deployment/idempotent-queue-consumers` |
| Dev compose = production engines (PostgreSQL + RabbitMQ, pinned); `.env` config only | `deployment/local-dev-compose`, `deployment/env-connection-urls` |
| BannedApiAnalyzers as errors across all projects | enforcement layer |

## Design notes

- **The shared-host trap:** two modules in one process means two DbContexts. A plain
  `services.AddScoped<IUnitOfWork, EfUnitOfWork<X>>()` from each module would let the last
  registration win — Catalog handlers would silently save the Orders context. The module
  registration methods therefore wire each command handler to a unit of work bound to its
  own context, explicitly. Single-module hosts (`Orders.Worker`, `Outbox.Dispatch`) are free
  of the problem and register `IUnitOfWork` normally.
- **Publish before save:** `PlaceOrderCommandHandler` raises the domain event *before*
  `SaveChangesAsync`, so the reactor's outbox row joins the same transaction. With a direct
  broker publish (the lambda siblings' reactor), the order is reversed.
- **Parking, not endless nacking:** the consumer nacks transient failures into the DLX retry
  cycle; once the attempt budget is spent it *publishes* the message to the DLQ exchange and
  acks the original — a third nack would just cycle forever.
- **The API host has no broker client.** It writes outbox rows in its transactions; only
  `Outbox.Dispatch` and the workers speak AMQP. An API test needs PostgreSQL and nothing else.

## Run it locally

```bash
# infrastructure — the production engines
docker compose up -d

# schema (Flyway owns it; EF is runtime-only)
docker run --rm --network host -v $PWD/Common.Database/db:/flyway/sql flyway/flyway:10 \
  -url=jdbc:postgresql://localhost:5432/app -user=postgres -password=postgres migrate

# the processes (three terminals, or run what you need)
dotnet run --project App.Api            # http://localhost:5000
dotnet run --project Outbox.Dispatch    # drains outbox -> RabbitMQ
dotnet run --project Orders.Worker      # consumes order-placed-queue

# frontend dev server (proxies /v1 to :5000)
cd Web && npm ci && npm start

# watch a message flow
curl -s -X POST localhost:5000/v1/products -H 'content-type: application/json' \
  -d '{"sku":"SKU-1","name":"Widget"}'
curl -s -X POST localhost:5000/v1/orders -H 'content-type: application/json' \
  -d '{"productId":"<id from above>","quantity":2}'
# order row -> outbox row -> broker -> worker -> status Confirmed
# broker UI: http://localhost:15672 (guest/guest)
```

Full production topology on one machine:

```bash
docker network create infra
docker run -d --network infra --network-alias infra-postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=app postgres:16
docker run -d --network infra --network-alias infra-rabbitmq rabbitmq:4.1-management
docker run --rm --network infra -v $PWD/Common.Database/db:/flyway/sql flyway/flyway:10 \
  -url=jdbc:postgresql://infra-postgres:5432/app -user=postgres -password=postgres migrate
docker compose -f docker-compose.prod.yml up --build
# SPA + API on http://localhost:8080
```

## Tests

```bash
dotnet test                              # unit tests always run
# integration tiers activate via env vars (CI sets both):
#   TEST_DATABASE_URL  -> repository/API/outbox-row tests vs the Flyway schema
#   RABBITMQ_URL       -> provider round-trip, consumer, DLX parking, outbox drain
```
