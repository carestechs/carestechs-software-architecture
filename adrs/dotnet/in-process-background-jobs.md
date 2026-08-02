---
category: dotnet
stack: dotnet
status: Active
requires:
  - adrs/dotnet/async-all-the-way.md
conflicts_with:
  - adrs/deployment/queue-based-decoupling.md
last_reviewed: 2026-08-02
---

# In-Process Background Jobs

## Decision
Background work runs inside the application process: a hosted `BackgroundService` consumes a bounded `System.Threading.Channels.Channel<T>` for fire-and-forget work whose loss on restart is tolerable. Work that must survive restarts uses a persistent job store (Hangfire backed by the application's PostgreSQL) — not a message broker. One background-work model per system: adopting a broker means graduating to queue-based decoupling, not running both.

## Rationale
- At single-deployable scale, a broker (SQS, RabbitMQ, Redis) adds an infrastructure component, an ops surface, and at-least-once semantics the team must then honor — for work a `Channel<T>` hands to a hosted service in-process with zero moving parts.
- The honest cost is loss semantics: channel contents die with the process. Making that explicit — tolerable-loss work on channels, must-survive work in a PostgreSQL-backed job store — keeps the rung truthful without a broker.
- Alternatives considered: naked `Task.Run` fire-and-forget (rejected — unobserved exceptions, no backpressure, no shutdown story), an in-memory `ConcurrentQueue` polled by a timer (rejected — reinvents Channels poorly), a broker from day one (rejected at this rung — that is the queue-based-decoupling ADR, adopted when module decoupling or crash-safe pipelines are actually needed), Quartz.NET (viable for cron-shaped scheduling; Hangfire chosen for its PostgreSQL storage and dashboard).

## Constraints (non-negotiable for AI)
- Fire-and-forget work goes through a BOUNDED channel consumed by a `BackgroundService`. NEVER unbounded channels (memory is the queue limit) and NEVER naked `Task.Run` fire-and-forget.
- The consuming service MUST resolve dependencies per job through `IServiceScopeFactory` — NEVER inject scoped services (like a DbContext) into the singleton hosted service.
- Shutdown MUST drain: honor the stopping token, stop accepting new work, and finish or persist in-flight work within the host's shutdown timeout.
- Work that MUST NOT be lost on crash/restart goes to the persistent job store (Hangfire + PostgreSQL storage), with bounded retries — or the system graduates to `queue-based-decoupling`. NEVER put must-not-be-lost work on an in-memory channel.
- Job handlers are idempotent where the store retries them, and every job type declares explicit bounded retry behavior. NEVER infinite retries.
- Channel jobs carry plain data (ids, primitives, small DTOs). NEVER entities attached to a DbContext from the enqueuing request.
