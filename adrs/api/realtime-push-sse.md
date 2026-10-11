---
category: api
stack: any
status: Active
requires: []
conflicts_with:
  - adrs/deployment/iot-mqtt-push.md
last_reviewed: 2026-10-11
---

# Real-Time Push from a Single Hub: Server-Sent Events and Web Push

## Decision
Live updates to connected clients are server-sent events (SSE) streamed by the API process over the same origin and the same authentication as the REST API, resumable by `Last-Event-ID`, with in-process fan-out. Notifications that must reach a client that is not connected (a phone with the app closed) go through standard Web Push: the server holds the VAPID keys and the clients' subscriptions and sends a short payload that tells the client to reconnect and fetch. Scoped to deployments with one API process (or one sticky replica); more replicas mean a broker behind the same endpoints, not a different client contract.

## Rationale
- At single-hub scale (a handful of agents and clients) SSE is the simplest live channel that works everywhere: one long-lived HTTP response, automatic reconnect in every browser, no WebSocket upgrade path to secure, ordinary HTTP authentication. WebSocket is warranted when the client must stream data back; commands here are ordinary POSTs.
- Web Push is the only standards-based way to wake an installed web app. It replaces a third-party notification service with keys and subscriptions the hub owns, which matters for a tool whose point is independence from vendors.
- The managed-MQTT decision in this catalog (`iot-mqtt-push`) is right for multi-tenant fan-out on AWS and rejects self-hosted WebSocket fan-out at that scale; this ADR is the other end of the scale, and the two are exclusive per system.
- Alternatives considered: polling (kept as the degraded fallback the client uses when the stream is unavailable), WebSocket (acceptable later for bidirectional needs, same scoping), long-polling (rejected; SSE does the same with less code), third-party pub/sub or notification services (rejected; a vendor in the middle of every event and every command).

## Constraints (non-negotiable for AI)
- The event stream is `text/event-stream` on an authenticated endpoint of the API's origin. Every event carries an `id`; the server replays from `Last-Event-ID` out of a bounded retained window (state the window; the client falls back to a full fetch beyond it).
- Fan-out is in-process, and the retained window lives in the database or in memory with a declared loss semantics. NEVER a second service for this rung; more than one API replica means a broker behind the same endpoints, recorded as an ADR change.
- Web Push payloads carry an identifier and a kind, NEVER content: the client fetches what it needs over the authenticated API. Subscriptions are stored per device and removed on `410 Gone`; the VAPID private key is configuration, never in the repository.
- Clients treat the stream as a hint and the API as truth: every rendered state MUST be reachable by a fetch, so a missed event never shows a wrong screen.
- Heartbeat comment lines at a fixed interval keep intermediaries from closing idle streams, and the server bounds concurrent streams per principal.
