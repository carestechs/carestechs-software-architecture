---
category: database
stack: any
status: Active
requires:
  - adrs/deployment/queue-based-decoupling.md | adrs/python/celery-background-jobs.md
conflicts_with: []
last_reviewed: 2026-08-01
---

# Transactional Outbox with Latency Escape Hatch

## Decision
Events that must not be lost are written to an outbox table in the same database transaction as the state change that produced them. A scheduled dispatcher drains the outbox to the queue and marks rows dispatched. Latency-critical, user-facing notifications MAY bypass the outbox and enqueue directly — but only with a documented client-side reconciliation path.

## Rationale
- "Save the row, then enqueue" has a crash window between the two operations; either the state change exists with no event, or (enqueue-first) the event exists for a state change that rolled back. Writing the event in the same transaction closes the window; the dispatcher then guarantees at-least-once publication.
- The dispatcher runs on a schedule, and that cadence is a floor on event latency. Production experience: a one-minute dispatcher cadence blew a 1.5-second p50 real-time-notification budget by 20×. Tightening the schedule fights the platform; the honest answer is a documented two-tier model — outbox for correctness-critical flows, direct enqueue for latency-critical hints.
- The bypass is safe only because the direct-enqueued message is a *hint*, not the source of truth: the client reconciles authoritative state via a REST read on (re)connect, so a lost hint degrades latency, not correctness.
- Alternatives considered: distributed transactions across DB and queue (rejected — not supported by SQS and operationally hostile everywhere), change-data-capture pipelines (rejected at current scale — heavy infrastructure for the same at-least-once result), enqueue-inside-transaction-commit hooks (rejected — reintroduces the crash window it claims to close).

## Constraints (non-negotiable for AI)
- Outbox rows MUST be inserted in the same transaction as the state change. An outbox insert in a separate transaction is a bug, not a variant.
- The dispatcher and all consumers MUST be idempotent — the pipeline is at-least-once end to end. Consumers key on the event ID, not delivery count.
- Dispatched rows are marked, not deleted, until a retention window passes — the outbox is also the audit trail for "was this event published".
- Direct enqueue (bypassing the outbox) is allowed ONLY for notifications that are hints over authoritative state, and ONLY when the consumer has a reconciliation read path. Document the pairing at the enqueue site.
- NEVER dual-write the same event through both paths — pick outbox or direct per event type.
