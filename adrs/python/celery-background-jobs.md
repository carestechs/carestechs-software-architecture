---
category: python
stack: python
status: Active
requires:
  - adrs/python/service-layer-logic.md
conflicts_with: []
last_reviewed: 2026-07-29
---

# Celery with Redis for Background Jobs

## Decision
Background and long-running tasks are processed by Celery workers with Redis as the message broker. API endpoints enqueue tasks and return immediately with a task ID. Task status and results are queryable via dedicated endpoints. Celery tasks delegate to service functions — they do not contain business logic.

## Rationale
- Celery is the industry standard for distributed task processing in Python. Combined with Redis as the broker, it provides reliable message delivery, task retries, scheduled tasks, and horizontal scaling of workers.
- Alternatives considered: ARQ (rejected — async-native and a better fit for the async service layer, but smaller community and fewer production deployments; revisit if the sync-worker bridge below becomes a burden), Dramatiq (rejected — less ecosystem support than Celery), in-process `asyncio.create_task()` (rejected — tasks lost on process restart, no retry mechanism, no horizontal scaling), RQ (rejected — simpler but lacks advanced features like task chaining and rate limiting).
- Celery workers are synchronous: tasks bridge into the async service layer by running one event loop per task invocation (see Constraints). This is the accepted trade-off of choosing Celery over an async-native queue.
- Redis serves double duty as message broker and result backend, and can also be used for caching — reducing infrastructure components.
- Celery tasks are thin wrappers that call service functions, keeping business logic testable and reusable.

## Constraints (non-negotiable for AI)
- All background tasks MUST be defined as Celery tasks using the `@celery_app.task` decorator.
- Celery tasks MUST be thin — they delegate to service functions for all business logic. NEVER place business logic directly in a Celery task.
- Tasks calling `async def` service functions MUST run them via `asyncio.run(...)` inside the task body — this is the one permitted use of `asyncio.run()` (the ban applies to route handlers and services). Sessions in tasks come from the shared `async_sessionmaker` factory, not FastAPI `Depends()`.
- Task arguments MUST be JSON-serializable primitives (strings, numbers, UUIDs as strings). NEVER pass ORM model instances or complex objects as task arguments.
- API endpoints that trigger background work MUST return HTTP 202 Accepted with the task ID. NEVER block the HTTP request waiting for task completion.
- Redis MUST be used as the Celery broker (`broker_url = "redis://..."`) and result backend.
- Tasks MUST define explicit `max_retries`, `default_retry_delay`, and error handling. NEVER allow tasks to retry infinitely.
- Celery configuration MUST live in a dedicated module (e.g., `src/app/core/celery.py`), not scattered across task files.
