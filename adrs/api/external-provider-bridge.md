---
category: api
stack: any
status: Active
requires:
  - adrs/deployment/queue-based-decoupling.md
conflicts_with: []
last_reviewed: 2026-07-31
---

# External Provider Bridge Module

## Decision
All integration with a third-party platform API (messaging provider, payment processor, social API) is isolated in a single Bridge module — the ONLY module allowed to call the provider's endpoints. Egress: core modules enqueue provider-neutral messages; Bridge workers consume the queue and translate to provider API calls. Ingress: provider webhooks land on a thin Bridge HTTP endpoint that validates the signature (HMAC), normalizes payloads into internal contracts, and fans out via queues. Provider credentials, token refresh, and provider-state caches are owned by Bridge.

## Rationale
- Third-party APIs version, rate-limit, and reshape payloads on their own schedule. One egress point means one place absorbs that churn; core modules speak stable internal DTOs and survive provider API upgrades untouched.
- Queue decoupling on both directions makes provider latency and rate limits invisible to request paths, and gives retries/DLQs for free at the integration boundary — the least reliable part of the system.
- Signature validation before parsing is the webhook security boundary: unauthenticated payloads never reach deserialization, let alone business logic.
- Production lessons encoded here: provider-state caches (connection registrations, template approval status) need explicit TTL semantics that fail OPEN into a provider lookup — a cache miss must never drop a webhook; and cached provider status drifts, so a reconciliation path (scheduled or webhook-driven) must exist for every cached status field.
- Alternatives considered: each module calls the provider directly (rejected — N copies of auth, retries, and payload churn; impossible to rate-limit coherently), a shared HTTP client library (rejected — couples every module to provider payload shapes at compile time even if the transport is shared).

## Constraints (non-negotiable for AI)
- ONLY Bridge projects may reference the provider's SDK or call its endpoints. A provider URL or SDK type appearing in any other module is a defect.
- Webhook handlers MUST validate the provider's signature (HMAC or equivalent) before any parsing or persistence. Invalid signatures are rejected without side effects.
- Normalize at the edge: provider payload shapes MUST NOT cross out of Bridge — fan-out messages carry internal contracts only.
- Provider identifiers preserved for correlation (external message IDs, business account IDs) are carried as opaque strings on internal contracts — never re-parsed downstream.
- Provider-state caches MUST have explicit TTL and staleness semantics that fail open to a provider lookup, plus a reconciliation path for status drift. NEVER treat a cache miss as "does not exist".
- Provider credentials live in the secrets store and are read only by Bridge components.
