---
category: python
stack: python
family: in-process-background-work
status: Active
requires:
  - adrs/python/async-all-the-way.md
conflicts_with:
  - adrs/python/celery-background-jobs.md
last_reviewed: 2026-10-11
---

# In-Process Background Tasks

## Decision
Background work runs inside the API process: asyncio tasks started and stopped by the FastAPI lifespan, fed by a bounded `asyncio.Queue` for fire-and-forget work whose loss on restart is tolerable, and periodic loops for scheduled work. Work that must survive a restart is a row in the application's PostgreSQL (status, attempts, next attempt time), claimed and advanced by those loops, never an in-memory structure. One background-work model per system: adopting Celery or any broker means graduating to it, not running both.

## Rationale
- At single-deployable scale a broker and a worker container (Celery with Redis) add two processes, an operations surface and at-least-once semantics the team must then honour, for work an asyncio task does in-process with no moving parts.
- The honest cost is loss semantics, so the rule makes it explicit: tolerable-loss work on a bounded queue, must-survive work in rows. PostgreSQL is already there, and `SELECT ... FOR UPDATE SKIP LOCKED` claims a due row safely even if a second process ever appears.
- Alternatives considered: `asyncio.create_task` fire-and-forget without an owner (rejected; unobserved exceptions, no shutdown story, no backpressure), FastAPI `BackgroundTasks` (acceptable only for work tied to one request's lifetime; it is not a queue), APScheduler (viable for cron shapes; not required), Celery from day one (rejected at this rung; that is `celery-background-jobs`, adopted when work must leave the process).
- Mirror of the .NET `in-process-background-jobs` decision: same rung, same graduation.

## Constraints (non-negotiable for AI)
- Fire-and-forget work goes through a BOUNDED `asyncio.Queue` consumed by a task the lifespan owns. NEVER unbounded queues and NEVER orphan `create_task` calls whose result nobody awaits or logs.
- Shutdown MUST drain: on lifespan exit, stop accepting work, cancel loops cooperatively, and finish or persist in-flight work within a bounded timeout.
- Work that MUST NOT be lost on crash or restart is a row with `status`, `attempts` and `next_attempt_at`; the loop claims rows with `FOR UPDATE SKIP LOCKED` and advances them in a transaction. NEVER keep such work only in memory.
- Every persisted job type declares bounded retries with backoff, and handlers are idempotent per job id. NEVER infinite retries.
- Loops open their own database session per iteration. NEVER hold a request's session or one long-lived session across iterations.
- Queue items carry plain data (ids, primitives, small dicts). NEVER ORM instances.

## Examples

**Violation — fire-and-forget nobody owns:**
```python
@router.post("/events")
async def ingest(event: EventIn, db: Session = Depends(get_db)):
    asyncio.create_task(push_to_devices(event))   # exception lost, no shutdown, unbounded
```

**Compliant:**
```python
await app.state.push_queue.put(event.id)          # bounded asyncio.Queue(maxsize=1000)
# lifespan owns the consumer task; the row for a must-survive delivery is written in the same transaction
```
