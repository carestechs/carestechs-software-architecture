"""Seed local-dev users: python -m app.modules.identity.dev_seed"""
import asyncio

from app.core.database import async_session_factory
from app.modules.identity import service

USERS = [
    ("admin@example.com", "Admin123!", "admin"),
    ("agent@example.com", "Agent123!", "agent"),
]


async def main() -> None:
    async with async_session_factory() as session:
        async with session.begin():
            for email, password, role in USERS:
                try:
                    await service.create_user(session, email, password, role)
                    print(f"created {role}: {email} / {password}")  # noqa: T201 - operator script
                except Exception:
                    print(f"skipped {email} (already exists)")  # noqa: T201 - operator script


if __name__ == "__main__":
    asyncio.run(main())
