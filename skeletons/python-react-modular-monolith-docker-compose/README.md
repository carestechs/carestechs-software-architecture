# Golden Skeleton: python-react-modular-monolith-docker-compose

A minimal, **buildable** instance of `profiles/python-react-modular-monolith-docker-compose.md`.
Its job is to prove, in CI, that the profile's ADRs compose into a project that installs, lints,
tests, and builds. If a catalog change breaks this skeleton, the change — not the skeleton — is suspect.

## What it demonstrates

| Area | ADRs exercised |
|------|----------------|
| FastAPI app, one `catalog` module, contracts package | `python/fastapi-framework`, `python/modular-packages`, `python/service-layer-logic` |
| Async end-to-end: AsyncSession, asyncpg, async Alembic env | `python/async-all-the-way`, `python/sqlalchemy-async` |
| Pydantic DTOs with camelCase serialization | `python/pydantic-at-boundary` |
| `{ data, meta }` envelope on 2xx, Problem Details on errors | `api/rest-envelope`, `python/rfc7807-errors` |
| uuid PKs, timestamptz, snake_case, module-prefixed migration slug | `database/uuid-primary-keys`, `database/timestamptz-always`, `database/snake-case-naming` |
| stdlib logging: JSON handler, request-ID contextvar filter | `python/structured-logging` |
| pytest + pytest-asyncio, httpx ASGI client, per-test transaction rollback against real PostgreSQL | `python/pytest-testing` |
| ruff config copied from `enforcement/python/` | enforcement layer |
| React 19 + Vite + Tailwind v4 (`@theme`-era, no config file) + TanStack Query v5 (`isPending`) | `react/functional-components`, `react/tanstack-query`, `react/tailwind-shadcn` (partially) |
| Multi-stage Dockerfiles, dev-infra vs prod-app compose split, nginx SPA proxy | `deployment/docker-multi-stage-builds`, `deployment/local-dev-compose`, `deployment/container-per-process`, `deployment/nginx-spa-proxy`, `deployment/env-connection-urls` |
| CI: ruff + pytest (with a PostgreSQL service) + tsc + vite build + docker builds | `deployment/github-actions-ci` |

## Run it locally

```bash
# infrastructure
docker compose up -d

# backend
python -m venv .venv && .venv/bin/pip install -e ".[dev]"
.venv/bin/alembic upgrade head
.venv/bin/uvicorn app.main:app --reload

# tests (uses TEST_DATABASE_URL, defaults to localhost app_test — create it first:
#   docker compose exec postgres createdb -U postgres app_test)
.venv/bin/pytest

# frontend (separate terminal; proxies /api to :8000)
cd client && npm install && npm run dev
```

## Deliberately not demonstrated (yet)

- **Celery worker + Redis** (`python/celery-background-jobs`) — no background workload in the skeleton; compose omits Redis.
- **JWT auth / role-based authorization** (`api/jwt-bearer-auth`, `api/role-based-authorization`) — endpoints are anonymous.
- **A second module** — cross-module communication is shown only as the `contracts/` protocol shape.
- **shadcn/ui primitives** — the page uses no button/input/dialog primitives yet.
- **Offset pagination** (Optional tier) — the list endpoint returns all rows with `meta.totalCount`.

Additions must follow the profile's ADRs — this skeleton is held to the same constraints it demonstrates.
