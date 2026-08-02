---
category: deployment
stack: any
status: Active
requires:
  - adrs/deployment/queue-based-decoupling.md
conflicts_with:
  - adrs/deployment/eventbridge-domain-events.md
last_reviewed: 2026-08-02
---

# RabbitMQ as the Containerized Message Broker

## Decision

In containerized deployments (Docker Compose or Kubernetes), RabbitMQ is the production message broker behind the queue-provider abstraction. Point-to-point work queues use direct exchanges with one durable queue per concern; domain-event fan-out uses topic exchanges with one durable queue per consuming module. Retries and dead-lettering are declared as broker topology — dead-letter exchanges with TTL-based backoff — not written as handler code. The broker runs as an infrastructure container with a pinned version, a persistent volume, and the management UI enabled.

## Rationale

- The queue abstraction (`queue-based-decoupling.md`) stays the invariant: handlers never see the broker. Moving between this ADR and the AWS profiles' SQS/EventBridge changes one provider implementation and the compose file — application code is untouched. That symmetry is why this ADR and `eventbridge-domain-events.md` are declared mutually exclusive: they are the same architectural slot on different substrates, and one system gets one production transport.
- RabbitMQ is mature and operationally boring: quorum queues for durability, per-queue FIFO with a single consumer, dead-letter exchanges giving retry/backoff/DLQ without application machinery, and a management UI that answers "where did my message go" without extra tooling.
- Topic-exchange fan-out mirrors the EventBridge pattern one-to-one — same producer contract, one queue per consumer, independent replay per module.
- Alternatives considered: Redis Streams (lighter when Redis is already deployed, but consumer groups have no dead-letter concept, so retry/DLQ machinery migrates into application code — exactly what DLX avoids); Kafka (rejected for this system class — a partitioned log with consumer-managed offsets earns its operational weight only at event-sourcing/replay or extreme-throughput scale); NATS JetStream (viable and lighter, but a much smaller operational knowledge base); a cloud broker (defeats the vendor independence that motivates the containerized rung).

## Constraints (non-negotiable for AI)

- All cross-module async messaging MUST go through the queue-provider abstraction. Application code MUST NOT reference the RabbitMQ client library outside the provider implementation in the shared providers project.
- Queues MUST be durable and messages persistent. The broker container MUST pin a specific image version (never `latest`) and mount a persistent volume.
- Every work queue MUST declare a paired dead-letter queue via a dead-letter exchange. Retry policy — attempt count and backoff TTLs — MUST be expressed as topology, not as handler code.
- Consumers MUST use manual acknowledgements: ack after successful processing; nack without requeue routes to the DLQ (pairs with `idempotent-queue-consumers.md`).
- Domain-event fan-out MUST use a topic exchange with one durable queue per consuming module. Consumers MUST NOT share a fan-out queue.
- Topology (exchanges, queues, bindings, dead-letter wiring) MUST be declared idempotently by code or versioned config at startup — NEVER hand-created in the management UI.
- Every consumer MUST set an explicit prefetch count. Unbounded prefetch is forbidden.
- Message headers MUST carry the correlation ID end to end (see `correlation-propagation.md`).
