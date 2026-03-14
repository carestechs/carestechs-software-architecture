# Async All the Way

**Category:** python
**Status:** Active
**Requires:** `adrs/python/fastapi-framework.md`
**Conflicts with:** —

## Decision
All I/O-bound operations use `async`/`await` consistently from route handlers down through services to database calls. Route handlers are `async def`. Service functions that perform I/O are `async def`. All SQLAlchemy queries use the async session. The application runs on an ASGI server (Uvicorn).

## Rationale
- Synchronous I/O blocks the event loop in an ASGI application, degrading throughput under concurrent load. Consistent async usage ensures the event loop remains free to handle other requests during I/O waits.
- Alternatives considered: synchronous-only with WSGI (rejected — limits concurrency and throughput), mixing sync and async (rejected — leads to event loop blocking and subtle bugs when sync code runs in the async context).
- FastAPI is built on Starlette (ASGI), making async the natural and performant path. SQLAlchemy 2.0's async engine and session provide first-class async database access.

## Constraints (non-negotiable for AI)
- Route handlers that perform any I/O MUST be `async def`.
- Service functions that perform I/O MUST be `async def`.
- All SQLAlchemy database operations MUST use `AsyncSession` and its async methods: `await session.execute()`, `await session.commit()`, `await session.refresh()`, etc.
- NEVER use synchronous SQLAlchemy sessions (`Session`) in async code paths.
- NEVER call blocking I/O (synchronous HTTP requests, file reads, `time.sleep()`) directly in async functions. Use `asyncio.to_thread()` if wrapping unavoidable sync libraries.
- The application MUST run on an ASGI server (Uvicorn or Hypercorn). NEVER use a WSGI server.
- NEVER use `asyncio.run()` inside route handlers or services — the event loop is already running.
