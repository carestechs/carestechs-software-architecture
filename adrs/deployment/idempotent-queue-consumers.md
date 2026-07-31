---
category: deployment
stack: any
status: Active
requires:
  - adrs/deployment/queue-based-decoupling.md
conflicts_with: []
last_reviewed: 2026-07-31
---

# Idempotent Consumers, DLQs, and Redrive Discipline

## Decision
Every queue consumer is written for at-least-once delivery: handlers are idempotent, keyed on the message's stable event ID. Every queue has a dead-letter queue with a bounded `maxReceiveCount` and an alarm on DLQ depth. Visibility timeout exceeds the handler's worst case. Batch consumers report per-item failures so one poison record does not recycle the whole batch. DLQ messages are triaged by operators and redriven after the fix — never auto-deleted, never auto-replayed.

## Rationale
- At-least-once is the queue's contract, not an edge case: redelivery happens on timeouts, worker crashes, and scaling events. A handler that is only correct on first delivery is incorrect.
- The DLQ is the difference between a poison message costing one alarm and costing the whole pipeline: without it, an unparseable message redelivers forever, starving the queue and burning compute.
- Bounded retries with parking (rather than infinite retry) turns "bad deploy produced bad messages" into a redrive after the rollback instead of data loss or a stuck pipeline.
- Alternatives considered: exactly-once via FIFO dedup windows (rejected as a general answer — dedup windows are minutes, redelivery risks are not), distributed idempotency locks (rejected — a conditional write/upsert keyed on event ID does the same with no coordination service), auto-redrive on a timer (rejected — replaying before the fix lands just re-parks everything).

## Constraints (non-negotiable for AI)
- Handlers MUST be safe under redelivery: conditional writes/upserts keyed on the event ID, or an idempotency record checked-and-set in the same transaction as the effect. NEVER assume first delivery.
- Every queue declares a DLQ with `maxReceiveCount` (3–5 for transient-failure workloads) in infrastructure code. A queue without a DLQ fails review.
- Visibility timeout MUST exceed the handler's worst-case duration (including downstream timeouts). NEVER heartbeat-extend visibility in a loop to simulate exactly-once.
- Batch event sources MUST use partial-batch failure reporting (`ReportBatchItemFailures` or equivalent) — failing the whole batch for one record multiplies redeliveries of healthy messages.
- Alarm on DLQ depth > 0 and on source-queue message age. A silent DLQ is an unmonitored outage.
- DLQ handling is triage → fix → redrive. NEVER wire a DLQ to automatic reprocessing, and NEVER let DLQ retention expire unexamined messages.
- Consumers distinguish permanent failures (validation — park immediately, don't burn retries) from transient ones (timeouts — let redelivery retry) where the failure mode is knowable.
