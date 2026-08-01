"""Celery tasks are thin wrappers over service functions
(adrs/python/celery-background-jobs.md)."""
import asyncio
from uuid import UUID

from app.core.celery import celery_app
from app.core.database import engine
from app.modules.orders import service


@celery_app.task(bind=True, max_retries=3, default_retry_delay=30)
def generate_order_receipt(self, order_id: str) -> dict:
    """Id-only JSON argument; delegates to the async service via asyncio.run —
    the one permitted use of asyncio.run (adrs/python/celery-background-jobs.md)."""

    async def _run() -> dict:
        try:
            return await service.build_receipt(UUID(order_id))
        finally:
            # Pooled connections are bound to the event loop that created them,
            # and every task invocation runs its own loop — dispose inside the
            # loop so the next task starts with a clean pool.
            await engine.dispose()

    try:
        return asyncio.run(_run())
    except Exception as exc:  # transient failures retry with a bounded policy
        raise self.retry(exc=exc) from exc
