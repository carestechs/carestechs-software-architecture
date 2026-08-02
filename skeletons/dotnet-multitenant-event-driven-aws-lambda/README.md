# Golden Skeleton: dotnet-multitenant-event-driven-aws-lambda

A minimal, **buildable** instance of the flagship profile — phase 2 of the AWS-skeletons
plan. Same honesty contract as the clean-arch-lambda skeleton: CI executes what can run
without AWS, stands in for what has no free emulator, lints the rest, and this table says
which is which.

## Proven vs stand-in vs linted

| Claim | How it is handled |
|-------|-------------------|
| **Database-per-tenant + DbUp provisioning**: `CREATE DATABASE` from validated identifiers, full embedded migration history replayed per tenant | **Executed in CI** — the real `TenantDatabaseProvisioner` provisions two tenants; data in tenant A is structurally invisible through tenant B's scope |
| **Schema-per-module**: DbUp creates the `messaging` schema; the DbContext declares it; the journal stays in `public` | **Executed in CI** — inside the provisioning tests |
| **Module facade**: one public `IMessagingModuleApi`, snapshot records, everything else `internal` (`InternalsVisibleTo` only for the module's own Data layer and tests) | **Executed + compile-enforced** |
| **DynamoDB hot path**: tenant-scoped partition keys (`{org}#{wkp}#{conversation}`), Query-by-key only | **Executed in CI** — against the official DynamoDB Local |
| **Transactional outbox**: row written in the tenant transaction; dispatcher drains to SQS with the persisted correlation id; rows marked, never deleted | **Executed in CI** — end to end against ElasticMQ |
| **Cognito** (`cognito-authentication`): pre-token tenant-claim enrichment | **Stand-in** — the real handler runs against a recorded trigger-event fixture and the real DynamoDB directory; the API validates the same claim contract from a test issuer. Real Cognito pools/triggers are phase-3 |
| **IoT MQTT push** (`iot-mqtt-push`): custom-authorizer policy scoped to the caller's own topic subtree | **Stand-in** — the policy builder is a pure function, tested for scoping (wildcards only below the user segment). Real IoT wiring is phase-3 |
| SAM stack (API fn, queue + DLQ + redrive, scheduled outbox dispatcher, DDB tables) | **Linted only** — `sam validate --lint`, `cfn-lint`, `sam build` |
| Deployment, IAM, API Gateway authorizers, real Lambda invocation | **Not proven** — phase-3 real-AWS smoke |

## What it demonstrates

- One module (`Messaging.{Domain,Application,Data,Api,Infra}`) in full flagship shape:
  rich `Conversation` entity, facade-only public surface, per-tenant unit-of-work factory
  (`OpenForTenant` validating identifiers at the boundary), lowercase columns inside the
  module schema, EF runtime-only against DbUp-managed DDL.
- The tenancy machinery: validated `OrgId`/`WorkspaceId` value types, the connection
  builder, the provisioner, and the **global tenant directory in DynamoDB** — the one
  deliberately global dataset.
- `Outbox.Dispatch` as its own one-concern Lambda project (scheduled in the template),
  draining per-tenant outboxes with `FOR UPDATE SKIP LOCKED`.
- `Auth.PreTokenGeneration` and `Notification.Authorizer` as thin, testable handlers —
  the stand-in boundary is exactly the AWS wiring, never the logic.
- Enforcement carried down: BannedApiAnalyzers as errors, 0-warning build. One
  supply-chain catch is encoded in `Common.Database.csproj`: `dbup-postgresql`
  transitively pins a vulnerable Npgsql 3.2.7 (GHSA-x9vc-6hfv-hg8c); a direct reference
  lifts it to the patched line.

## Run it locally

```bash
# infrastructure: postgres + DynamoDB Local + ElasticMQ (or run only what you need)
docker compose -f - up -d <<'YAML'
services:
  postgres:
    image: postgres:16
    environment: { POSTGRES_PASSWORD: postgres }
    ports: ["5432:5432"]
  dynamodb:
    image: amazon/dynamodb-local:latest
    ports: ["8000:8000"]
  elasticmq:
    image: softwaremill/elasticmq-native:latest
    ports: ["9324:9324"]
YAML

# tests: pure tests always run; integration provisions tenant databases itself
TEST_DATABASE_URL="Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres" \
DDB_ENDPOINT_URL=http://localhost:8000 AWS_ENDPOINT_URL=http://localhost:9324 \
AWS_ACCESS_KEY_ID=test AWS_SECRET_ACCESS_KEY=test AWS_REGION=us-east-1 \
dotnet test

# operator migration entry (same DbUp pipeline the provisioner uses)
dotnet run --project Common.DatabaseCli -- "Host=localhost;Port=5432;Database=tenant_acme_main;Username=postgres;Password=postgres"

# the API (test-issuer JWTs carry org/workspace claims)
dotnet run --project Messaging.Api
```

## Deliberately not demonstrated (yet)

- **The external provider Bridge, MQTT delivery, machine-to-machine auth, S3 media** —
  Recommended-tier breadth; the Required tier is what this skeleton proves.
- **Per-module DB roles** — the schema separation is provisioned; the role split is the
  documented next enforcement step (see `adrs/database/schema-per-module.md`).
- **Real AWS anything** — phase 3, when a sandbox account exists.

This skeleton is held to the same constraints it demonstrates.
