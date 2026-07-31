# Golden Skeleton: python-react-modular-monolith-docker-compose

A minimal, **buildable** instance of `profiles/python-react-modular-monolith-docker-compose.md`.
Its job is to prove, in CI, that the profile's ADRs compose into a project that installs, lints,
tests, and builds. If a catalog change breaks this skeleton, the change — not the skeleton — is suspect.

## What it demonstrates

| Area | ADRs exercised |
|------|----------------|
| FastAPI app, `catalog` + `orders` modules, contracts package | `python/fastapi-framework`, `python/modular-packages`, `python/service-layer-logic` |
| Cross-module by id: `orders` stores a plain UUID `product_id` (no FK/relationship) and resolves it through `contracts/catalog.py`; the provider is injected via `create_router()` at the composition root — the orders package never imports the catalog package | `dotnet/cross-module-by-id` (family rule), `python/modular-packages` |
| Async end-to-end: AsyncSession, asyncpg, async Alembic env | `python/async-all-the-way`, `python/sqlalchemy-async` |
| Pydantic DTOs with camelCase serialization | `python/pydantic-at-boundary` |
| `{ data, meta }` envelope on 2xx, Problem Details on errors | `api/rest-envelope`, `python/rfc7807-errors` |
| uuid PKs, timestamptz, snake_case, module-prefixed migration slug | `database/uuid-primary-keys`, `database/timestamptz-always`, `database/snake-case-naming` |
| stdlib logging: JSON handler, request-ID contextvar filter | `python/structured-logging` |
| JWT auth: 15-min HS256 access tokens (explicit alg allowlist, iss/aud, 60s leeway), refresh rotation with family revocation on reuse, httpOnly `SameSite=Strict` cookie scoped to `/api/auth`, CSRF header guard on refresh | `api/jwt-bearer-auth` |
| Two-layer authorization: `require_role("admin")` endpoint gate on product writes; order ownership enforced in the service next to the data (404 for "not yours", so IDs leak nothing) | `api/role-based-authorization` |
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

# seed dev users (admin@example.com / Admin123!, agent@example.com / Agent123!)
.venv/bin/python -m app.modules.identity.dev_seed

# tests (uses TEST_DATABASE_URL, defaults to localhost app_test — create it first:
#   docker compose exec postgres createdb -U postgres app_test)
.venv/bin/pytest

# frontend (separate terminal; proxies /api to :8000)
cd client && npm install && npm run dev
```

## Endpoint access matrix

Every endpoint's access level is explicit (adrs/api/role-based-authorization.md):

| Endpoint | Access |
|----------|--------|
| `POST /api/auth/login` | anonymous |
| `POST /api/auth/refresh` | refresh cookie + `X-Requested-With` header (CSRF guard) |
| `GET /api/products`, `GET /api/products/{id}` | anonymous — public catalog, deliberate |
| `POST /api/products` | role `admin` |
| `POST /api/orders` | any authenticated user (`created_by` stamped from claims) |
| `GET /api/orders/{id}` | owner or `admin` — service-layer check, 404 otherwise |
| `GET /health` | anonymous |

## Deliberately not demonstrated (yet)

- **Celery worker + Redis** (`python/celery-background-jobs`) — no background workload in the skeleton; compose omits Redis.
- **shadcn/ui primitives** — the page uses no button/input/dialog primitives yet.
- **An orders UI and a login UI** — the second module and the auth stack demonstrate backend rules; the React client only shows the public catalog. (A frontend would keep access tokens in memory only — never localStorage/sessionStorage.)
- **Logout / token revocation endpoint** — the refresh-token model supports it (revoke the family); only the endpoint is omitted.
- **Offset pagination** (Optional tier) — the list endpoint returns all rows with `meta.totalCount`.

Additions must follow the profile's ADRs — this skeleton is held to the same constraints it demonstrates.
