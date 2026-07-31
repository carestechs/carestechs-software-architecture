---
category: deployment
stack: any
status: Active
requires:
  - adrs/deployment/queue-based-decoupling.md
conflicts_with: []
last_reviewed: 2026-07-31
---

# FIFO Queues Only for Per-Aggregate Ordering

## Decision
Standard queues are the default everywhere. A FIFO queue is introduced only where out-of-order processing of the SAME aggregate is a correctness bug (a chatbot conversation's steps, a state machine's transitions). The message group ID is the aggregate's ID — never a constant — so ordering is per aggregate and throughput parallelizes across aggregates. Deduplication IDs are explicit, derived from the stable event ID; content-based deduplication stays off.

## Rationale
- FIFO costs real throughput: per-group ordering means one slow aggregate serializes behind itself, and per-queue throughput ceilings are orders of magnitude below Standard. Paying that for workloads where handlers are already idempotent and order-tolerant is waste.
- Group-per-aggregate is the entire trick: global ordering (constant group ID) turns the queue into a single lane and is almost never the actual requirement — the requirement is "this conversation's events in order", not "all events in order".
- Explicit dedup IDs beat content-based deduplication because payload evolution (a new field, a serializer change) silently breaks content hashing, and the 5-minute dedup window then admits duplicates exactly when producers retry.
- Alternatives considered: Standard queue + sequence numbers with consumer-side reordering (rejected — reimplements FIFO with buffers and gaps), pessimistic locks per aggregate in the consumer (rejected — moves the serialization into the database with timeout failure modes).

## Constraints (non-negotiable for AI)
- Every FIFO queue carries a comment in the infrastructure template naming the aggregate whose ordering it protects. No justification, no FIFO.
- Message group ID MUST be the aggregate ID. NEVER a constant, an environment name, or a tenant ID alone (a tenant is not an aggregate — grouping by tenant serializes the whole tenant).
- Deduplication ID MUST be set explicitly from the event's stable identity. NEVER enable content-based deduplication.
- Do not mix ordered and order-indifferent traffic on one FIFO queue — the order-indifferent messages inherit the throughput ceiling for nothing.
- Consumers of FIFO queues MUST NOT block on cross-aggregate work (a lookup that waits on another group's outcome deadlocks the lane).
- FIFO consumers still follow the idempotent-consumer rules — the dedup window is minutes; it narrows duplicates, it does not eliminate them.
