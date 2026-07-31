---
category: deployment
stack: any
status: Active
requires:
  - adrs/deployment/queue-based-decoupling.md
conflicts_with: []
last_reviewed: 2026-07-31
---

# Domain Facts on an Event Bus; Directed Work on Queues

## Decision
Two kinds of async traffic, two channels. Directed work — "module A needs module B to do something" — rides SQS queues owned by the consumer (the queue-based-decoupling rule). Domain facts — "this happened", where the producer neither knows nor cares who listens — are published to an event bus (EventBridge) with versioned detail-types (`entity.action.v1`). Consumers attach their own rules routing bus events into their own queues; the producer's code never changes when audiences change.

## Rationale
- SQS is point-to-point: broadcasting a fact over queues means the producer maintains the list of interested queues, which couples it to its audience and turns every new consumer (analytics, an integration, a new module) into a producer change. A bus inverts that: consumers subscribe, producers publish.
- The rule-into-queue pattern keeps every consumer's reliability machinery intact — bus events land in the consumer's queue and inherit its DLQ, retries, and idempotency discipline.
- Versioned detail-types are the schema contract: consumers evolve independently, so `v2` is published alongside `v1` during migration windows rather than mutating a shape under subscribers.
- Alternatives considered: SNS fan-out (workable subset — no content filtering or archive/replay; EventBridge chosen where it exists in the platform), publishing facts to a shared "events" queue that one dispatcher re-routes (rejected — reinvents the bus with a single point of failure), letting consumers poll producer APIs (rejected — latency and load for nothing).

## Constraints (non-negotiable for AI)
- Bus events are FACTS in past tense (`conversation.closed.v1`). NEVER publish commands or requests to the bus — directed work goes to the consumer's queue explicitly.
- The producer MUST NOT know its consumers: no consumer names, queue URLs, or feature flags per audience in producing code.
- Detail-types are versioned from day one. Breaking a payload shape means publishing the new version alongside the old for a migration window — never mutating `v1`.
- Event payloads carry identifiers plus a minimal snapshot of what changed. Consumers needing more resolve it through the owning module's contract/API — the bus is not a data replication channel.
- Every consumer rule targets a queue owned by that consumer (inheriting its DLQ and idempotency rules). NEVER target a Lambda directly from a rule — that bypasses the reliability layer.
- Bus delivery is at-least-once and unordered: consumers apply the idempotent-consumer rules, and order-sensitive reactions belong on a FIFO queue fed by the rule, not on assumptions about bus behavior.
