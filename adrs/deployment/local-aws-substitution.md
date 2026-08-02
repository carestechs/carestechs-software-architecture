---
category: deployment
stack: any
status: Active
requires: []
conflicts_with: []
last_reviewed: 2026-08-02
---

# Local Substitutes for AWS Services

## Decision

Local development and CI never require an AWS account. Every AWS managed service in the architecture gets a designated local substitute, reached through the same seam the production implementation uses — an endpoint override (`AWS_ENDPOINT_URL`) or a DI-registered provider — never an environment branch in application code. Services with no faithful emulator get a documented stand-in strategy (pure-function extraction plus recorded fixtures) instead of a pretend one.

## Rationale

- Zero-credential onboarding keeps the inner loop fast: clone, start the infrastructure containers, run. No IAM user provisioning, no cost meter on a developer laptop.
- Substituting at the seam keeps dev/prod parity honest: the code path that runs locally is the code path that ships; only composition differs. An `if (env == "local")` branch in application code is a second code path that production never executes.
- Protocol-faithful emulators beat hand-rolled fakes for integration tests: a real SQS wire protocol (ElasticMQ) or a real DynamoDB engine (DynamoDB Local) exercises serialization, pagination, and error shapes that a fake would hide.
- Alternatives considered: LocalStack as an all-in-one emulator (viable, but heavyweight, and current images license-gate startup even for free-tier services); a shared dev AWS account for the inner loop (rejected — credentials, cost, and contention; verifying against real AWS is what staging is for); mocking the AWS SDK (fine at unit level, never a substitute for an integration path).
- Honesty over coverage: some services (Cognito flows, IAM policy evaluation) only verify against real AWS. Pretending a fake covers them creates false confidence; naming the gap routes those checks to staging where they belong.

### Substitution ladder

| AWS service | Local substitute | Fidelity notes |
|-------------|------------------|----------------|
| RDS PostgreSQL | Docker container pinned to the production major version | Full SQL parity; IAM auth not exercised |
| SQS | ElasticMQ (native image) for integration tests; the queue-provider seam may poll a lightweight HTTP queue for the inner dev loop | Wire-compatible including FIFO; no IAM |
| DynamoDB | DynamoDB Local | Real engine; single node, no autoscaling or throttling behavior |
| S3 | MinIO, or a filesystem implementation behind the storage seam | Presigned-URL semantics differ subtly — verify in staging |
| Secrets Manager / SSM | File-based providers behind the same interfaces | No rotation locally |
| Lambda hosting | The same handlers in a local host (Kestrel/uvicorn for APIs, a console polling loop for workers); `sam local invoke` only to smoke-test packaging | Cold starts, memory limits, and timeouts not reproduced |
| API Gateway | Direct local HTTP host | Throttling, authorizers, and request mapping not reproduced |
| Cognito | No emulator: a dev token issuer signs JWTs with a dev key; trigger handlers run as pure functions against recorded event fixtures | The full auth flow verifies only against real AWS |
| EventBridge | The event-bus seam backed by the local queue | Rule and filter matching not exercised locally |
| IoT Core (MQTT) | Local Mosquitto broker, or a no-op publisher behind the push seam | A custom authorizer is testable only as a pure function |

## Constraints (non-negotiable for AI)

- Local development and the test suite MUST run without AWS credentials. Onboarding is: clone, start infrastructure containers, run.
- Substitution MUST happen at a seam — an endpoint override or a DI registration in the composition root. Domain and application code MUST NOT branch on environment.
- Substitutes with a real engine (PostgreSQL, DynamoDB Local) MUST be pinned to the production major version in versioned config (the compose file), not `latest`.
- Integration tests MUST prefer a protocol-faithful emulator over a hand-rolled fake whenever one exists for the service.
- Every service with no faithful emulator MUST get all three: its logic extracted as a pure function, recorded-fixture tests, and an explicit "verified only against real AWS" note in the module or repository README.
- Local secrets MUST be dummy values; committed compose and config files MUST NOT contain real credentials.
- The project MUST document its substitution table — which substitute stands in for each service, and that substitute's known fidelity gap.
