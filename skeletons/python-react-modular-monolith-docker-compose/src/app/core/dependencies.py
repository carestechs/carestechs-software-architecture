from collections.abc import AsyncIterator

from sqlalchemy.ext.asyncio import AsyncSession

from app.core.database import async_session_factory


async def get_session() -> AsyncIterator[AsyncSession]:
    """Yield a request-scoped session; commit on success, roll back on error."""
    async with async_session_factory() as session:
        async with session.begin():
            yield session
