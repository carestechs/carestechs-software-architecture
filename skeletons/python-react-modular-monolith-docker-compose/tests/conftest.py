import os

# The Settings singleton reads the environment at import time, so the test
# database URL must be in place before anything from `app` is imported.
TEST_DATABASE_URL = os.environ.get(
    "TEST_DATABASE_URL", "postgresql+asyncpg://postgres:postgres@localhost:5432/app_test"
)
os.environ["DATABASE_URL"] = TEST_DATABASE_URL
# Secure cookies are not sent over http by the test client; debug mode keeps the
# refresh cookie testable (production always runs behind TLS)
os.environ["DEBUG"] = "1"

from collections.abc import AsyncIterator

import pytest
from alembic import command
from alembic.config import Config
from httpx import ASGITransport, AsyncClient
from sqlalchemy.ext.asyncio import AsyncSession, async_sessionmaker, create_async_engine

from app.core.dependencies import get_session
from app.main import create_app


@pytest.fixture(scope="session", autouse=True)
def apply_migrations() -> None:
    config = Config("alembic.ini")
    command.upgrade(config, "head")
    yield
    command.downgrade(config, "base")


@pytest.fixture
async def db_session() -> AsyncIterator[AsyncSession]:
    """Per-test isolation: everything runs inside a transaction that is rolled back."""
    engine = create_async_engine(TEST_DATABASE_URL)
    async with engine.connect() as connection:
        transaction = await connection.begin()
        factory = async_sessionmaker(
            bind=connection, expire_on_commit=False, join_transaction_mode="create_savepoint"
        )
        async with factory() as session:
            yield session
        await transaction.rollback()
    await engine.dispose()


@pytest.fixture
async def users(db_session: AsyncSession) -> dict:
    """Seed one admin and two agents inside the rolled-back test transaction."""
    from app.modules.identity import service as identity_service

    return {
        "admin": await identity_service.create_user(
            db_session, "admin@example.com", "Admin123!", "admin"
        ),
        "agent": await identity_service.create_user(
            db_session, "agent@example.com", "Agent123!", "agent"
        ),
        "agent2": await identity_service.create_user(
            db_session, "agent2@example.com", "Agent123!", "agent"
        ),
    }


@pytest.fixture
async def client(db_session: AsyncSession) -> AsyncIterator[AsyncClient]:
    app = create_app()

    async def override_session() -> AsyncIterator[AsyncSession]:
        yield db_session

    app.dependency_overrides[get_session] = override_session
    transport = ASGITransport(app=app)
    async with AsyncClient(transport=transport, base_url="http://test") as test_client:
        yield test_client
