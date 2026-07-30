# Queue-Based Module Decoupling

**Category:** deployment
**Status:** Active
**Requires:** `adrs/dotnet/event-driven-reactors.md`
**Conflicts with:** —

## Decision

Modules that need to trigger work in other modules do so by enqueuing messages to a queue. An `IQueueProvider` abstraction supports multiple implementations: `HttpQueueProvider` for local development (polling a lightweight queue server) and `SqsQueueProvider` for production (Amazon SQS). Worker services dequeue messages and process them. This decouples modules at the deployment level — the producer and consumer can scale, deploy, and fail independently.

## Rationale

- Queue-based decoupling ensures that a failure in the consuming module does not affect the producing module. If the consumer is down, messages accumulate in the queue and are processed when it recovers. This is critical for reliability in distributed systems.
- Alternatives considered: direct HTTP calls between modules (rejected — creates synchronous coupling, cascading failures, and retry complexity), shared database polling (rejected — inefficient, creates contention), in-process event bus for cross-module communication (rejected — doesn't work when modules are separate Lambda functions or services).
- The `IQueueProvider` abstraction allows the same reactor code to work in development (HTTP queue) and production (SQS) without code changes. Only the DI registration differs.
- Dead Letter Queues (DLQ) capture failed messages after a configurable retry count (default: 3), preventing poison messages from blocking the queue.
- The interface names here (`IQueueProvider`, `IJsonSerializer`) are the .NET reference implementation; the pattern itself (queue abstraction, DLQ, idempotent consumers, dev/prod provider swap) applies unchanged to other stacks.

## Constraints (non-negotiable for AI)

- Cross-module asynchronous communication MUST go through a queue via `IQueueProvider`. NEVER call another module's handlers directly for asynchronous work.
- Queue provider implementations MUST be swappable via DI: `HttpQueueProvider` for development, `SqsQueueProvider` for production.
- Production queues MUST have a Dead Letter Queue (DLQ) configured with a maximum receive count (default: 3 retries).
- Queue names MUST come from the configuration/parameters provider (see `adrs/deployment/aws-secrets-parameters.md` — SSM in production, file-based in development), never hardcoded.
- Worker services that consume queues MUST be idempotent — processing the same message twice MUST produce the same result.
- Messages MUST be serialized as JSON using the shared `IJsonSerializer`.
- In development, a lightweight queue server (`Common.QueueServer`) MUST be available for local testing of the full produce/consume flow.
