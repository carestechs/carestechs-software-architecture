---
category: python
stack: python
family: async-all-the-way
status: Active
requires:
  - adrs/python/fastapi-framework.md
conflicts_with: []
last_reviewed: 2026-07-29
---

# Async All the Way

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

## Examples

**Violation — sync session and blocking HTTP inside the event loop:**
```python
@router.get("/products/{product_id}")
def get_product(product_id: UUID, session: Session = Depends(get_session)):
    data = requests.get(PRICE_API).json()  # blocks the event loop
    return session.get(Product, product_id)
```

**Compliant:**
```python
@router.get("/products/{product_id}", response_model=ProductRead)
async def get_product(product_id: UUID, session: AsyncSession = Depends(get_session)):
    async with httpx.AsyncClient() as client:
        data = (await client.get(PRICE_API)).json()
    return await product_service.get_product(session, product_id)
```
