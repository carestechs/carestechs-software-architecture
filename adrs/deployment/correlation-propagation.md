---
category: deployment
stack: any
status: Active
requires:
  - adrs/deployment/queue-based-decoupling.md
conflicts_with: []
last_reviewed: 2026-07-31
---

# Correlation ID Propagation Across Async Hops

## Decision
A correlation ID is minted once, at true ingress — an HTTP request without one, a validated provider webhook, a scheduled trigger firing. From there it rides every internal hop: queue messages carry it in envelope metadata/message attributes, workers restore it into their logging scope before handling, outbox rows persist it, and real-time push payloads include it. One user action is one correlation ID across every Lambda, queue, and log group it touches.

## Rationale
- In an event-driven system the interesting failures span hops: a webhook that fanned out to three queues and died in the fourth worker. Per-request logging (the structured-logging ADRs) makes each hop searchable; only a propagated correlation ID makes the *journey* searchable with one query.
- Minting only at ingress is the discipline that makes the ID meaningful: a worker that generates a fresh ID on a missing attribute silently cuts the trace — better to log the absence loudly and continue with a marked orphan ID.
- Alternatives considered: managed distributed tracing (X-Ray/OTel — complementary, and worth layering on; rejected as the *only* mechanism because sampling drops exactly the rare failing journey you need, and trace context doesn't reach human-readable logs by itself), inferring flow from timestamps (rejected — concurrency makes it archaeology).

## Constraints (non-negotiable for AI)
- Every enqueue MUST copy the current correlation ID into the message (attribute or envelope field). A message without one is a bug at the producer, not the consumer.
- New correlation IDs are minted ONLY at true ingress points. Consumers finding a missing ID log a warning and mark the ID as orphaned (`orphan-{new-id}`) — they do not silently start a new trace.
- Every consumer restores the correlation ID into its logging scope BEFORE the first business log line (see the structured-logging ADRs for the scope mechanism).
- The correlation ID is observability metadata: NEVER use it for authorization, idempotency, or business keys — it has no uniqueness or integrity guarantees.
- Outbox rows persist the correlation ID of the producing transaction; the dispatcher stamps it onto published messages.
- DLQ triage output (logs, dashboards) MUST surface the correlation ID so a parked message can be traced back to its origin.
