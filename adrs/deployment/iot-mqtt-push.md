---
category: deployment
stack: any
status: Active
requires:
  - adrs/api/cognito-authentication.md | adrs/api/jwt-bearer-auth.md
conflicts_with: []
last_reviewed: 2026-07-31
---

# Real-Time Push via Managed MQTT (IoT Core)

## Decision
Real-time notifications to browser and desktop clients ride managed MQTT (AWS IoT Core) over WebSocket. Each user gets a private topic subtree (`{app}/{org}/{workspace}/user/{userId}/...`). A custom authorizer exchanges the caller's application JWT for an IoT policy scoped to that user's own subtree — cross-user and cross-tenant subscriptions are denied at SUBSCRIBE time. Publishes come from a queue-fed dispatcher worker, never inline from request handlers. Pushed payloads are hints: the client reconciles authoritative state through the REST API on connect and reconnect.

## Rationale
- Self-hosted WebSocket fan-out (connection tables, heartbeats, resume logic) is a distributed system of its own; a managed MQTT broker provides fan-out, backpressure, and connection lifecycle as a service, and browsers speak it over WebSocket with standard client libraries.
- Alternatives considered: API Gateway WebSocket APIs (rejected — hand-rolled connection registry in DynamoDB plus per-connection posts recreate exactly the machinery the broker provides), polling (rejected as primary — seconds of latency and wasted read load; retained only as degraded fallback), server-sent events from Lambda (rejected — execution-duration economics).
- Per-user topic scoping enforced by the authorizer turns tenant isolation into broker policy: a compromised client cannot even subscribe outside its subtree.
- Hint-not-state is the resilience model: missed messages during a disconnect degrade latency, not correctness, because the REST reconciliation read is the source of truth.

## Constraints (non-negotiable for AI)
- Topic structure MUST embed app, tenant, and user identifiers, and the authorizer MUST scope the granted policy to the caller's own subtree (wildcards only below the user segment).
- The custom authorizer validates the SAME JWT the REST APIs accept — no separate credential for the push channel.
- Publishes go through the queue-fed dispatcher. NEVER publish to the broker inline from an API handler — enqueue the notification and return.
- Fan-out is assignment-scoped: push only to users who own or are assigned the underlying resource — never broadcast to a workspace when the event concerns one user.
- Payloads carry identifiers and event type, not authoritative state. Clients MUST reconcile via REST on connect/reconnect; UI state MUST NOT be built solely from pushed payloads.
- Internal broker endpoints and policy names are discovered at deploy time and injected as stack parameters — never hardcoded in runtime code.
