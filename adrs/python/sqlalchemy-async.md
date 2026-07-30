---
category: python
stack: python
status: Active
requires:
  - adrs/python/async-all-the-way.md
conflicts_with: []
last_reviewed: 2026-07-29
verify_against:
  - SQLAlchemy 2
---

# SQLAlchemy 2.0 Async with Alembic

## Decision
SQLAlchemy 2.0 with async engine and sessions is the ORM. Alembic handles all database migrations. Each module defines its models in `models.py`, and all models inherit from a shared declarative base. The async session is provided to services via FastAPI's dependency injection.

## Rationale
- SQLAlchemy 2.0 provides a mature, battle-tested ORM with first-class async support via `AsyncSession` and `create_async_engine`. It is the standard Python ORM for PostgreSQL.
- Alternatives considered: Tortoise ORM (rejected — smaller community, fewer features), SQLModel (rejected — still maturing, limited relationship support), raw asyncpg (rejected — no ORM benefits, manual SQL management).
- Alembic's migration system supports auto-generation from model changes, versioned migration files, and branch-based migrations for modular development.
- The `asyncpg` driver provides the fastest async PostgreSQL connectivity for Python.

## Constraints (non-negotiable for AI)
- All SQLAlchemy models MUST use the 2.0 declarative style with `mapped_column()` and type annotations.
- All models MUST inherit from a shared `Base` class (declarative base) defined in a shared location.
- Database sessions MUST be `AsyncSession` instances created from `async_sessionmaker`.
- In HTTP request scope, sessions MUST be injected into services via FastAPI `Depends()`, using a dependency that yields a session and handles commit/rollback. Outside HTTP scope (Celery tasks, CLI scripts), obtain sessions directly from the shared `async_sessionmaker` factory.
- All database operations MUST use `await` with async session methods.
- Migrations MUST be managed by Alembic. NEVER modify database schema outside of Alembic migrations.
- Alembic's `env.py` MUST support the async engine (run migrations via `connection.run_sync(...)`, or configure a separate synchronous URL for migrations only).
- The database engine MUST use `create_async_engine` with the `asyncpg` driver (connection string: `postgresql+asyncpg://...`).
- Models MUST live in each module's `models.py` file. The shared `Base` MUST be importable by all modules.

## Examples

**Violation — legacy 1.x query API and sync execution:**
```python
users = session.query(User).filter_by(active=True).all()
```

**Compliant:**
```python
result = await session.execute(select(User).where(User.active.is_(True)))
users = result.scalars().all()
```
