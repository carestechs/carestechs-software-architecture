# Stack Profile: Python + React Modular Monolith

**Status:** Active
**Assumes:** Python 3.12+, FastAPI 0.110+, React 19+, PostgreSQL 16+, SQLAlchemy 2.0+, Tailwind CSS 4+, Vite 6+

## Overview

A curated set of ADRs for building a modular monolith backend with Python/FastAPI and a React SPA frontend. This is the industry-standard stack for AI-powered API products, developer tools, and data-intensive applications where the Python ecosystem's AI/ML libraries provide a decisive advantage. ADRs are categorized by how essential they are to the stack's coherence.

---

## Solution Structure

```
myapp/
├── pyproject.toml                          # Project metadata, dependencies (uv/poetry)
├── alembic.ini                             # Alembic migration config
├── docker-compose.yml                      # PostgreSQL, Redis, app services
│
├── src/
│   └── app/
│       ├── main.py                         # FastAPI app creation, router registration, middleware
│       ├── config.py                       # Settings via pydantic-settings (env vars)
│       │
│       ├── core/                           # Cross-cutting infrastructure
│       │   ├── database.py                 # AsyncEngine, async_sessionmaker, Base
│       │   ├── celery.py                   # Celery app configuration
│       │   ├── dependencies.py             # Shared FastAPI dependencies (get_db_session, etc.)
│       │   └── exceptions.py               # Global exception handlers, Problem Details
│       │
│       ├── contracts/                      # Shared interfaces for cross-module communication
│       │   ├── catalog.py                  # ICatalogService protocol
│       │   └── identity.py                 # IIdentityService protocol
│       │
│       ├── modules/
│       │   ├── catalog/                    # Example feature module
│       │   │   ├── __init__.py
│       │   │   ├── router.py              # FastAPI APIRouter endpoints
│       │   │   ├── service.py             # Business logic
│       │   │   ├── models.py              # SQLAlchemy entities
│       │   │   ├── schemas.py             # Pydantic request/response DTOs
│       │   │   └── dependencies.py        # Module-specific FastAPI dependencies
│       │   │
│       │   └── identity/                  # Another feature module (same structure)
│       │       ├── __init__.py
│       │       ├── router.py
│       │       ├── service.py
│       │       ├── models.py
│       │       ├── schemas.py
│       │       └── dependencies.py
│       │
│       └── migrations/                    # Alembic migrations
│           ├── env.py
│           └── versions/
│
├── client/                                # React SPA
│   ├── src/
│   │   ├── main.tsx                       # App entry point, QueryClientProvider
│   │   ├── App.tsx                        # Root component, router setup
│   │   │
│   │   ├── components/                    # Shared reusable components
│   │   │   └── ui/                        # shadcn/ui components
│   │   │       ├── button.tsx
│   │   │       ├── input.tsx
│   │   │       └── dialog.tsx
│   │   │
│   │   ├── features/                      # Feature-based route folders
│   │   │   ├── catalog/
│   │   │   │   ├── CatalogList.tsx
│   │   │   │   ├── CatalogDetail.tsx
│   │   │   │   └── api.ts                # TanStack Query hooks + fetch functions
│   │   │   └── auth/
│   │   │       ├── Login.tsx
│   │   │       └── api.ts
│   │   │
│   │   ├── hooks/                         # Shared custom hooks
│   │   ├── lib/                           # Utilities (cn(), api client, etc.)
│   │   │   └── utils.ts
│   │   └── styles.css                     # Global Tailwind imports only
│   │
│   ├── tailwind.config.ts
│   ├── vite.config.ts
│   ├── tsconfig.json
│   └── package.json
│
└── tests/
    ├── conftest.py                        # Shared fixtures (async test client, test DB)
    ├── modules/
    │   ├── catalog/
    │   └── identity/
    └── integration/
```

---

## Required (core to this stack — do not omit)

These ADRs define the fundamental architecture. Removing any of them breaks the coherence of the stack.

| ADR | Summary | Depends On |
|-----|---------|------------|
| `adrs/python/fastapi-framework.md` | FastAPI as the web framework. Async-native, auto OpenAPI docs, Pydantic validation. | — |
| `adrs/python/modular-packages.md` | Feature modules as Python packages under `src/app/modules/`. Clear boundaries, shared contracts. | `fastapi-framework` |
| `adrs/python/service-layer-logic.md` | Route handlers are thin. All business logic in service functions/classes. | — |
| `adrs/python/pydantic-at-boundary.md` | Pydantic schemas for all request/response payloads. Never expose ORM models. | `service-layer-logic` |
| `adrs/python/async-all-the-way.md` | All I/O uses async/await. AsyncSession for DB. Uvicorn as ASGI server. | `fastapi-framework` |
| `adrs/python/sqlalchemy-async.md` | SQLAlchemy 2.0 async ORM with Alembic migrations. asyncpg driver. | `async-all-the-way` |
| `adrs/react/functional-components.md` | Functional components with hooks only. Feature-based folder organization. | — |

## Recommended (strong defaults — can be swapped with noted alternatives)

These are battle-tested defaults. You can swap them, but you should have a good reason.

| ADR | Summary | Alternative |
|-----|---------|-------------|
| `adrs/python/celery-background-jobs.md` | Celery + Redis for background task processing. Tasks delegate to services. | ARQ or Dramatiq (smaller ecosystem) |
| `adrs/database/uuid-primary-keys.md` | All PKs are UUIDs. No auto-increment. | Auto-increment integers (simpler but less secure for external APIs) |
| `adrs/database/snake-case-naming.md` | snake_case tables/columns. Native to both Python and PostgreSQL. | — (already the natural convention for Python + PostgreSQL) |
| `adrs/database/timestamptz-always.md` | All datetimes are timestamptz. Python uses `datetime` with UTC timezone. | timestamp without timezone (loses timezone context) |
| `adrs/api/rest-envelope.md` | All responses wrapped in `{ data, meta }` envelope. | Flat responses with pagination in headers |
| `adrs/api/jwt-bearer-auth.md` | JWT Bearer tokens. Short-lived access + rotated refresh. | API key auth (simpler for developer-facing APIs) |
| `adrs/react/tanstack-query.md` | TanStack Query for all server state. Caching, refetching, cache invalidation. | SWR (fewer features) or raw useEffect (not recommended) |
| `adrs/react/tailwind-shadcn.md` | Tailwind CSS + shadcn/ui components. Full ownership, no runtime dependency. | Material UI or Chakra UI (heavier, opinionated) |

## Optional (pick based on project needs)

These address specific concerns that not every project has.

| ADR | Summary | When to Include |
|-----|---------|-----------------|
| `adrs/database/soft-deletes.md` | Soft deletion via nullable `deleted_at` column. | Projects needing audit trails or undo capability |
| `adrs/api/offset-pagination.md` | Offset pagination with page/pageSize/sortBy/sortDir. Requires `rest-envelope`. | Any project with list endpoints |

---

## Key Cross-Cutting Concerns

When using this stack, these patterns emerge from the combination of ADRs:

- **Naming consistency:** Python snake_case attributes map naturally to snake_case database columns and camelCase JSON (via Pydantic `alias_generator` or `model_config`). No naming translation layer needed on the backend.
- **Time handling:** Backend stores UTC `datetime` objects with `tzinfo=timezone.utc`, database uses `timestamptz`, frontend converts to local display time.
- **ID strategy:** UUIDs flow end-to-end: generated via `uuid.uuid4()` in Python, stored as `uuid` in PostgreSQL, serialized as strings in JSON.
- **Auth flow:** React app stores JWT in memory or httpOnly cookie, sends via `Authorization` header, FastAPI validates with dependency-injected auth guards.
- **Module isolation:** Each module is a Python package with its own router, service, models, and schemas. Cross-module communication is by UUID + shared contract protocol only.
- **Validation chain:** Pydantic validates at the API boundary (route handlers). SQLAlchemy enforces at the database boundary (model constraints). Services enforce business rules in between.
- **Background jobs:** Celery tasks are thin wrappers that call the same service functions used by route handlers, ensuring consistent behavior whether triggered by HTTP or by a background job.
- **Error handling:** FastAPI exception handlers return RFC 7807-style Problem Details responses. Pydantic validation errors are automatically formatted as 422 responses with field-level details.

## Development Workflow

- **Local development first:** Set up local development immediately after the base project structure exists (FastAPI app running, database connected, Alembic initialized, one module scaffolded). The application must start, serve the OpenAPI docs, and accept requests before adding feature code. Use Docker Compose for PostgreSQL and Redis.
- **Dependency management:** Use `uv` (recommended) or `poetry` for Python dependency management. Pin all dependencies in `pyproject.toml`.
- **Type checking:** Use `mypy` or `pyright` in strict mode. Python's type hints combined with Pydantic provide strong type safety.
