# Stack Profile: Python + FastAPI Simple Monolith (Single Server)

**Status:** Active
**Assumes:** Python 3.12+, FastAPI 0.110+, SQLAlchemy 2.0+ with Alembic, PostgreSQL 16+, React 19+ with Vite 6+ and Tailwind CSS 4+ for the client, Docker Compose v2+ on one server

## Overview

A curated set of ADRs for building a small product as ONE Python package serving both its API and its React client from a single container, next to a PostgreSQL container, on one server. This is the Python entry rung of the catalog, the sibling of `dotnet-angular-simple-monolith-single-server`: the fastest build-debug-deploy loop the catalog endorses, with the cross-cutting discipline kept intact (service layer, Pydantic at the boundary, Problem Details, the response envelope, UUID and timestamptz, structured logging, CI). Stripping architecture never means stripping discipline.

What this profile deliberately does NOT have: module packages, a contracts package, Celery or Redis, queues or an outbox, per-module schemas, an nginx container, managed identity, or any cloud machinery. Background work runs in-process with explicit loss semantics. The client is served by the API itself: one origin, no CORS, one application container.

Authentication at this rung is opaque hashed tokens for every non-human caller (agents, scripts, personal devices) through `machine-to-machine-auth`; interactive human login with JWTs is optional and added when there are users who log in.

**The graduation path is the point.** The service-layer and Pydantic rules here are exactly the ones `python-react-modular-monolith-docker-compose` requires, so when growth arrives (more than ~3 developers stepping on each other, clearly distinguishable domains, onboarding pain) moving to that profile is a restructuring PR: feature folders become module packages. Its Recommended tier then carries the next rung (Celery, outbox, enforced boundaries).

---

## Solution Structure

```
myapp/
├── pyproject.toml                  # one project, pinned dependencies (uv)
├── alembic.ini
├── Dockerfile                      # Node stage builds the client -> python:slim runtime with the bundle
├── docker-compose.yml              # dev: postgres only (the app runs on the host)
├── docker-compose.prod.yml         # prod: app container + postgres, one server
├── .env.example
│
├── src/app/
│   ├── main.py                     # app factory, lifespan (background tasks), routers, SPA fallback
│   ├── config.py                   # pydantic-settings from env vars
│   ├── core/
│   │   ├── database.py             # one engine, one Base, session dependency
│   │   ├── auth.py                 # token authorizer (hashed opaque tokens)
│   │   ├── errors.py               # Problem Details handlers
│   │   ├── logging.py              # JSON lines, correlation id
│   │   └── tasks.py                # bounded queue + periodic loops owned by the lifespan
│   ├── features/
│   │   ├── <feature>/              # router.py, service.py, models.py, schemas.py
│   │   └── ...
│   ├── migrations/                 # one Alembic history
│   └── static/                     # client bundle copied in by the Dockerfile
│
├── client/                         # React + Vite (+ PWA manifest and service worker when chosen)
│
└── tests/                          # pytest, httpx ASGI client, real PostgreSQL per test
```

---

## Required (core to this stack — do not omit)

These ADRs define the fundamental architecture. Removing any of them breaks the coherence of the stack.

<!-- generated from profiles/profiles.toml — edit the manifest and run scripts/generate_profiles.py -->
| ADR | Summary | Depends On |
|-----|---------|-------------|
| `adrs/python/single-package-app.md` | ONE application package, folder per feature, one SQLAlchemy metadata with one Alembic history. Service-layer and Pydantic-boundary rules apply in full - graduation to modular packages stays a restructuring PR. | `fastapi-framework` |
| `adrs/python/fastapi-framework.md` | FastAPI as the web framework. Async-native, auto OpenAPI docs, Pydantic validation. | — |
| `adrs/python/service-layer-logic.md` | Route handlers are thin. All business logic in service functions/classes. | — |
| `adrs/python/pydantic-at-boundary.md` | Pydantic schemas for all request/response payloads. Never expose ORM models. | `service-layer-logic` |
| `adrs/python/async-all-the-way.md` | All I/O uses async/await. AsyncSession for DB. Uvicorn as ASGI server. | `fastapi-framework` |
| `adrs/python/sqlalchemy-async.md` | SQLAlchemy 2.0 async ORM with Alembic migrations. asyncpg driver. | `async-all-the-way` |
| `adrs/python/rfc7807-errors.md` | RFC 9457 Problem Details for all errors. Global exception handlers override FastAPI's default error shape. | `fastapi-framework` |
| `adrs/api/rest-envelope.md` | All responses wrapped in `{ data, meta }` envelope. | — |
| `adrs/api/machine-to-machine-auth.md` | Opaque hashed tokens for every non-human caller - agents, scripts and personal devices alike: identifiable prefix, server stores a hash, scoped, revocable, `last_used_at` recorded. The default authentication at this rung. | — |
| `adrs/react/functional-components.md` | Functional components with hooks only. Feature-based folder organization. | — |
| `adrs/deployment/docker-multi-stage-builds.md` | One image: a Node stage builds the client, a python:slim runtime stage holds the app and the bundle. | — |
| `adrs/deployment/env-connection-urls.md` | All config via env vars. External services via connection URLs. Pydantic BaseSettings validates at startup. | — |
| `adrs/deployment/local-dev-compose.md` | `docker-compose.yml` for local infra, `docker-compose.prod.yml` for app services on shared network. | `docker-multi-stage-builds`, `env-connection-urls` |
| `adrs/deployment/spa-served-by-api.md` | Client bundle built in the Dockerfile's Node stage, served by FastAPI (static mount plus `index.html` fallback registered after the API routers). One container, one origin, no CORS. | `docker-multi-stage-builds` |

## Recommended (strong defaults — can be swapped with noted alternatives)

These are battle-tested defaults. You can swap them, but you should have a good reason.

<!-- generated from profiles/profiles.toml — edit the manifest and run scripts/generate_profiles.py -->
| ADR | Summary | Alternative |
|-----|---------|-------------|
| `adrs/python/in-process-background-tasks.md` | asyncio tasks owned by the lifespan: a bounded queue for tolerable-loss work, periodic loops for scheduled work, PostgreSQL rows claimed with SKIP LOCKED for work that must survive restarts. One background-work model per system - a broker means graduating. | `celery-background-jobs` with Redis (the next rung) |
| `adrs/python/pytest-testing.md` | pytest + pytest-asyncio, httpx ASGI test client, per-test DB isolation. | unittest (not recommended) |
| `adrs/python/structured-logging.md` | stdlib logging, per-module loggers, JSON formatter, correlation-ID middleware. | structlog (heavier, viable) |
| `adrs/database/uuid-primary-keys.md` | All PKs are UUIDs. No auto-increment. | Auto-increment integers (simpler but less secure) |
| `adrs/database/snake-case-naming.md` | snake_case tables/columns. Native to both Python and PostgreSQL. | — (already the natural convention for Python + PostgreSQL) |
| `adrs/database/timestamptz-always.md` | All datetimes are timestamptz. Python uses `datetime` with UTC timezone. | timestamp without timezone (loses timezone context) |
| `adrs/deployment/container-per-process.md` | The app (API + client bundle, one process) and PostgreSQL as separate containers. | Several processes in one container (harder to scale, restart, and reason about) |
| `adrs/deployment/github-actions-ci.md` | CI gate on every push/PR: lint (enforcement configs), typecheck, tests, build. Deploys are separate workflows. | GitLab CI / Azure DevOps (if repos move) |
| `adrs/react/tanstack-query.md` | TanStack Query for all server state. Caching, refetching, cache invalidation. | SWR (fewer features) or raw useEffect (not recommended) |
| `adrs/react/tailwind-shadcn.md` | Tailwind CSS + shadcn/ui components. Full ownership, no runtime dependency. | Material UI or Chakra UI (heavier, opinionated) |

## Optional (pick based on project needs)

<!-- generated from profiles/profiles.toml — edit the manifest and run scripts/generate_profiles.py -->
| ADR | Summary | When to Include |
|-----|---------|-------------|
| `adrs/api/realtime-push-sse.md` | Server-sent events from the API process for live updates (same origin, same auth, resumable by Last-Event-ID, in-process fan-out) and standard Web Push with hub-owned VAPID keys for clients that are not connected. Single-replica scope; more replicas mean a broker behind the same endpoints. | Dashboards people watch change, agents reporting in, anything that must reach a phone with the app closed |
| `adrs/react/pwa-installable-client.md` | Web manifest, a service worker that caches the shell and never API data, the Web Push subscription lifecycle, served from the API's origin. Installable on phone and desktop; works unchanged in a plain tab. | A client people install on a phone, or that needs background notifications or an offline shell |
| `adrs/deployment/private-network-exposure.md` | Reachable only on a private overlay network (tailnet or VPN): server bound to the overlay, TLS from it, application tokens unchanged as the second lock. No public name, no open port. | Personal or team tools whose users can all join the network; swap for a public endpoint with TLS when they cannot |
| `adrs/api/jwt-bearer-auth.md` | JWT Bearer tokens. Short-lived access + rotated refresh. | Human users who log in interactively; the token model above already covers agents and personal devices |
| `adrs/api/offset-pagination.md` | Offset pagination with page/pageSize/sortBy/sortDir. Requires `rest-envelope`. | Any project with list endpoints |
| `adrs/database/soft-deletes.md` | Soft deletion via nullable `deleted_at` column. | Projects needing audit trails or undo capability |

---

## Deployment: Docker Compose on one server

This profile's deployment mode is one `docker compose` project on one host:

- **Two containers.** The application image (API plus the built client, one process, `uvicorn`) and PostgreSQL. No worker, no Redis, no nginx. The image is built by the multi-stage Dockerfile: a Node stage builds the client, the runtime stage holds Python, the app and the bundle.
- **Configuration by environment.** `DATABASE_URL`, token pepper, VAPID keys when push is on, log level. Nothing in the image; `.env` on the server or the orchestrator's secrets.
- **Migrations on deploy.** `alembic upgrade head` runs as a one-shot command against the production database before the new container serves traffic.
- **Exposure is a separate choice.** Default is the overlay network only (`private-network-exposure`): bind the overlay address, TLS from it, no public port. A public endpoint with TLS termination is the swap when users cannot join the network; the application does not change.
- **Scaling is vertical first.** The app is stateless apart from the in-process queue (declared loss semantics) and the SSE retained window (database or memory, declared). A second replica is the moment `realtime-push-sse` and `in-process-background-tasks` say to put a broker behind the same endpoints, which is also the moment to consider the modular profile.
- **Backups are the database's.** `pg_dump` on a schedule from the host; the application container holds nothing to back up.

## Key Cross-Cutting Concerns

- **One origin, no CORS:** the client bundle is served by FastAPI (static mount plus `index.html` fallback after the API routers). A CORS policy appearing anywhere signals drift.
- **Alembic is the schema story:** one history, migrations committed with the change, applied on deploy.
- **Background-work loss semantics are explicit:** bounded `asyncio.Queue` for tolerable-loss work, PostgreSQL rows claimed with `SKIP LOCKED` for must-survive work; a broker means graduating, not adding one here.
- **Tokens, not sessions, by default:** every caller holds an opaque token with a prefix, stored hashed, scoped and revocable; `last_used_at` is recorded. Human JWT login is the optional addition.
- **Relationships are allowed** between features' models; there are no module boundaries yet. The discipline that must hold anyway: thin routers, services behind functions or classes with explicit inputs, Pydantic at the boundary, feature folders coherent. That is what makes graduation mechanical.
- **Live data when chosen:** SSE from the API process, resumable; Web Push to wake the installed client; the API is always the truth the client re-fetches.
- **Naming:** snake_case everywhere in Python and PostgreSQL, camelCase in JSON through Pydantic aliases.

## Development Workflow

### Local Development Commands

```bash
# infrastructure (postgres only; the app runs on the host)
docker compose up -d

# migrations, then the API with hot reload (serves on http://localhost:8000)
uv run alembic upgrade head
uv run uvicorn app.main:app --reload

# client dev server (separate terminal; proxies /api to :8000)
cd client && npm install && npm run dev

# tests (real PostgreSQL through TEST_DATABASE_URL)
uv run pytest
```

### Production Deployment

```bash
# on the server: build and start the app + postgres
docker compose -f docker-compose.prod.yml up -d --build

# apply migrations against the production database
docker compose -f docker-compose.prod.yml run --rm app alembic upgrade head

# verify
curl https://myapp.<overlay-domain>/health
```
