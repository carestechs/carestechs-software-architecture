# Stack Profile: Python + React Modular Monolith with AI Agent (Docker Compose)

**Status:** Active
**Assumes:** Python 3.12+, FastAPI 0.110+, React 19+, PostgreSQL 16+ (with pgvector), SQLAlchemy 2.0+, Tailwind CSS 4+, Vite 6+, Redis, Docker 24+, Docker Compose v2+

## Overview

A curated set of ADRs for building a modular monolith backend with Python/FastAPI and a React SPA frontend, extended with AI agent capabilities, deployed via Docker Compose. This stack builds on the base `Python + React Modular Monolith` profile and adds ADRs for LLM integration, tool calling, RAG, and conversation management. This is the industry-standard stack for AI-powered developer tools, research automation, and intelligent API products. ADRs are categorized by how essential they are to the stack's coherence.

---

## Solution Structure

```
myapp/
├── pyproject.toml                          # Project metadata, dependencies (uv/poetry)
├── alembic.ini                             # Alembic migration config
├── Dockerfile                              # Backend multi-stage build (python:slim)
├── .dockerignore                           # Build context exclusions
├── docker-compose.yml                      # Dev: PostgreSQL (pgvector) + Redis only
├── docker-compose.prod.yml                 # Prod: API + worker + frontend on shared infra network
├── .env.example                            # Dev environment variable template
├── .env.production.example                 # Prod environment variable template
│
├── src/
│   └── app/
│       ├── main.py                         # FastAPI app, router registration, LLM provider setup
│       ├── config.py                       # Settings via pydantic-settings (env vars, LLM config)
│       │
│       ├── core/                           # Cross-cutting infrastructure
│       │   ├── database.py                 # AsyncEngine, async_sessionmaker, Base
│       │   ├── celery.py                   # Celery app configuration
│       │   ├── dependencies.py             # Shared FastAPI dependencies (get_db_session, etc.)
│       │   ├── exceptions.py               # Global exception handlers, Problem Details
│       │   └── llm.py                      # LLM client factory, provider adapters
│       │
│       ├── contracts/                      # Shared interfaces for cross-module communication
│       │   ├── catalog.py                  # ICatalogService protocol
│       │   ├── identity.py                 # IIdentityService protocol
│       │   └── ai.py                       # IAIService protocol (for other modules to call AI)
│       │
│       ├── modules/
│       │   ├── catalog/                    # Example feature module
│       │   │   ├── __init__.py
│       │   │   ├── router.py
│       │   │   ├── service.py
│       │   │   ├── models.py
│       │   │   ├── schemas.py
│       │   │   └── dependencies.py
│       │   │
│       │   ├── identity/                   # Another feature module (same structure)
│       │   │   ├── __init__.py
│       │   │   ├── router.py
│       │   │   ├── service.py
│       │   │   ├── models.py
│       │   │   ├── schemas.py
│       │   │   └── dependencies.py
│       │   │
│       │   └── ai/                         # AI agent module
│       │       ├── __init__.py
│       │       ├── router.py               # Chat, exploration, conversation endpoints
│       │       ├── service.py              # LLM orchestration, RAG pipeline, synthesis
│       │       ├── models.py               # Conversation, Message, DocumentEmbedding
│       │       ├── schemas.py              # ChatRequest, ChatResponse, ConversationDto
│       │       ├── dependencies.py         # AI-specific dependencies (LLM client, etc.)
│       │       └── tools/                  # AI tool adapters (thin wrappers over services)
│       │           ├── __init__.py
│       │           ├── catalog_search.py
│       │           └── order_lookup.py
│       │
│       └── migrations/                     # Alembic migrations
│           ├── env.py
│           └── versions/
│
├── client/                                 # React SPA
│   ├── src/
│   │   ├── main.tsx                        # App entry point, QueryClientProvider
│   │   ├── App.tsx                         # Root component, router setup
│   │   │
│   │   ├── components/                     # Shared reusable components
│   │   │   └── ui/                         # shadcn/ui components
│   │   │
│   │   ├── features/                       # Feature-based route folders
│   │   │   ├── catalog/
│   │   │   │   ├── CatalogList.tsx
│   │   │   │   ├── CatalogDetail.tsx
│   │   │   │   └── api.ts
│   │   │   ├── chat/                       # AI chat feature
│   │   │   │   ├── Chat.tsx
│   │   │   │   ├── ChatHistory.tsx
│   │   │   │   └── api.ts
│   │   │   └── auth/
│   │   │       ├── Login.tsx
│   │   │       └── api.ts
│   │   │
│   │   ├── hooks/                          # Shared custom hooks
│   │   ├── lib/                            # Utilities (cn(), api client, etc.)
│   │   └── styles.css                      # Global Tailwind imports only
│   │
│   ├── Dockerfile                           # Multi-stage: Node build → nginx
│   ├── nginx.conf                           # SPA serving + API reverse proxy
│   ├── tailwind.config.ts
│   ├── vite.config.ts
│   └── package.json
│
├── scripts/
│   └── verify-docker.sh                     # Deployment smoke test
│
└── tests/
    ├── conftest.py                         # Shared fixtures (async test client, test DB)
    ├── modules/
    │   ├── catalog/
    │   ├── identity/
    │   └── ai/
    └── integration/
```

---

## Required (core to this stack — do not omit)

These ADRs define the fundamental architecture. Removing any of them breaks the coherence of the stack.

<!-- generated from profiles/profiles.toml — edit the manifest and run scripts/generate_profiles.py -->
| ADR | Summary | Depends On |
|-----|---------|-------------|
| `adrs/python/fastapi-framework.md` | FastAPI as the web framework. Async-native, auto OpenAPI docs, Pydantic validation. | — |
| `adrs/python/modular-packages.md` | Feature modules as Python packages under `src/app/modules/`. Clear boundaries, shared contracts. | `fastapi-framework` |
| `adrs/python/service-layer-logic.md` | Route handlers are thin. All business logic in service functions/classes. | — |
| `adrs/python/pydantic-at-boundary.md` | Pydantic schemas for all request/response payloads. Never expose ORM models. | `service-layer-logic` |
| `adrs/python/async-all-the-way.md` | All I/O uses async/await. AsyncSession for DB. Uvicorn as ASGI server. | `fastapi-framework` |
| `adrs/python/sqlalchemy-async.md` | SQLAlchemy 2.0 async ORM with Alembic migrations. asyncpg driver. | `async-all-the-way` |
| `adrs/react/functional-components.md` | Functional components with hooks only. Feature-based folder organization. | — |
| `adrs/ai/ai-agent-module.md` | AI agent is a dedicated feature module (own project/package, own AI-owned tables, own tools area) following all modular monolith conventions. | `modular-monolith` / `modular-packages` |
| `adrs/ai/llm-abstraction.md` | All LLM/embedding calls through the stack's provider-agnostic abstraction (M.E.AI on .NET; Protocol adapters in Python). Provider SDKs only in the composition root. | `async-all-the-way` |
| `adrs/ai/tool-calling-via-services.md` | AI tools are thin adapters delegating to existing service interfaces via shared contracts. No business logic in tools; no destructive ops without confirmation. | `llm-abstraction` |
| `adrs/deployment/docker-multi-stage-builds.md` | All components packaged as Docker images with multi-stage builds. Slim final stages. | — |
| `adrs/deployment/env-connection-urls.md` | All config via env vars. External services via connection URLs. Pydantic BaseSettings validates at startup. | — |
| `adrs/deployment/container-per-process.md` | API, worker, and frontend as separate containers. Same image + different command for backend services. | `docker-multi-stage-builds` |
| `adrs/deployment/local-dev-compose.md` | `docker-compose.yml` for local infra, `docker-compose.prod.yml` for app services on shared network. | `docker-multi-stage-builds`, `env-connection-urls` |

## Recommended (strong defaults — can be swapped with noted alternatives)

These are battle-tested defaults. You can swap them, but you should have a good reason.

<!-- generated from profiles/profiles.toml — edit the manifest and run scripts/generate_profiles.py -->
| ADR | Summary | Alternative |
|-----|---------|-------------|
| `adrs/python/rfc7807-errors.md` | RFC 9457 Problem Details for all errors. Global exception handlers override FastAPI's default error shape. | Custom error envelope (not recommended) |
| `adrs/python/celery-background-jobs.md` | Celery + Redis for background task processing. Tasks delegate to services. | ARQ or Dramatiq (smaller ecosystem) |
| `adrs/python/pytest-testing.md` | pytest + pytest-asyncio, httpx ASGI test client, per-test DB isolation. | unittest (not recommended) |
| `adrs/python/structured-logging.md` | stdlib logging, per-module loggers, JSON formatter, correlation-ID middleware. | structlog (heavier, viable) |
| `adrs/deployment/github-actions-ci.md` | CI gate on every push/PR: lint (enforcement configs), typecheck, tests, build. Deploys are separate workflows. | GitLab CI / Azure DevOps (if repos move) |
| `adrs/database/uuid-primary-keys.md` | All PKs are UUIDs. No auto-increment. | Auto-increment integers (simpler but less secure for external APIs) |
| `adrs/database/snake-case-naming.md` | snake_case tables/columns. Native to both Python and PostgreSQL. | — (already the natural convention) |
| `adrs/database/timestamptz-always.md` | All datetimes are timestamptz. Python uses `datetime` with UTC timezone. | timestamp without timezone (loses timezone context) |
| `adrs/api/rest-envelope.md` | All responses wrapped in `{ data, meta }` envelope. | Flat responses with pagination in headers |
| `adrs/api/jwt-bearer-auth.md` | JWT Bearer tokens. Short-lived access + rotated refresh. | API key auth (simpler for developer-facing APIs) |
| `adrs/react/tanstack-query.md` | TanStack Query for all server state. Caching, refetching, cache invalidation. | SWR (fewer features) or raw useEffect (not recommended) |
| `adrs/react/tailwind-shadcn.md` | Tailwind CSS + shadcn/ui components. Full ownership, no runtime dependency. | Material UI or Chakra UI (heavier, opinionated) |
| `adrs/ai/rag-pgvector.md` | RAG with pgvector in the existing PostgreSQL: chunk, embed via the abstraction, cosine search with an IVFFlat/HNSW index, delimited context injection. | Dedicated vector DB (Pinecone/Qdrant) at larger scale |
| `adrs/ai/conversation-history.md` | Conversations/messages persisted in AI-module tables; token-aware pruning through a single context builder; system prompt and latest user message always survive. | Prune-only vs persisted-summary flagged system message |
| `adrs/deployment/nginx-spa-proxy.md` | Nginx serves built SPA and reverse-proxies `/api/` to backend. `try_files` for client-side routing. | Serving SPA from backend framework or separate CDN |
| `adrs/deployment/idempotent-queue-consumers.md` | At-least-once discipline: idempotent handlers keyed on event ID, DLQ on every queue, triage-fix-redrive. | None viable — an SQS pipeline without DLQs is an unmonitored outage |
| `adrs/deployment/correlation-propagation.md` | One correlation ID from ingress through every queue hop and log scope. | Managed tracing only (X-Ray/OTel — complementary, sampling-limited) |
| `adrs/database/transactional-outbox.md` | Correctness-critical events written in-transaction, drained by a scheduled dispatcher; latency-critical hints may bypass with a reconciliation path. | Direct enqueue everywhere (accepts lost events on crash) |
| `adrs/database/schema-per-module.md` | Each module owns a PG schema; ORM default-schema per module; optional per-module DB roles. | Single shared schema with review-enforced boundaries (Pattern B — drifts under team growth) |

## Optional (pick based on project needs)

These address specific concerns that not every project has.

<!-- generated from profiles/profiles.toml — edit the manifest and run scripts/generate_profiles.py -->
| ADR | Summary | When to Include |
|-----|---------|-------------|
| `adrs/database/soft-deletes.md` | Soft deletion via nullable `deleted_at` column. | Projects needing audit trails or undo capability |
| `adrs/api/offset-pagination.md` | Offset pagination with page/pageSize/sortBy/sortDir. Requires `rest-envelope`. | Any project with list endpoints |
| `adrs/api/role-based-authorization.md` | Role gates at the endpoint layer + ownership checks in services. Deny by default. Requires `jwt-bearer-auth`. | Policy engine (OPA/Casbin) at larger scale |

---

## Key Cross-Cutting Concerns

When using this stack, these patterns emerge from the combination of ADRs:

- **Naming consistency:** Python snake_case attributes map naturally to snake_case database columns and camelCase JSON (via Pydantic `alias_generator` or `model_config`). No naming translation layer needed on the backend.
- **Time handling:** Backend stores UTC `datetime` objects with `tzinfo=timezone.utc`, database uses `timestamptz`, frontend converts to local display time.
- **ID strategy:** UUIDs flow end-to-end: generated via `uuid.uuid4()` in Python, stored as `uuid` in PostgreSQL, serialized as strings in JSON.
- **Auth flow:** React app stores JWT in memory or httpOnly cookie, sends via `Authorization` header, FastAPI validates with dependency-injected auth guards.
- **Module isolation:** Each module is a Python package with its own router, service, models, and schemas. Cross-module communication is by UUID + shared contract protocol only.
- **LLM provider independence:** Service code depends on abstract protocols. Swapping from OpenAI to Anthropic (or any other provider) is a composition root change only — no service code modifications.
- **AI tool boundary:** AI tools never contain business logic. They are thin adapters that delegate to the same service functions used by route handlers, ensuring consistent behavior whether triggered by HTTP or by LLM tool calling.
- **Vector storage colocation:** Embeddings live in PostgreSQL via pgvector alongside application data, avoiding a separate vector database deployment.
- **Context window safety:** Conversation history is always pruned before being sent to the LLM. Unbounded token usage is architecturally prevented.
- **RAG prompt hygiene:** Retrieved context is clearly delimited in prompts, chunk sizes are configurable, and all embeddings go through the provider-agnostic abstraction.
- **Background jobs:** Celery tasks (explorations, embedding generation, long-running AI operations) are thin wrappers that call the same service functions used by route handlers, ensuring consistent behavior.
- **Image reuse across process types:** The API server and Celery worker containers use the same Docker image with different `command` overrides. Only the frontend uses a separate image (nginx-based).
- **Environment parity via connection URLs:** The same application code connects to `postgresql://localhost:5432` in development and `postgresql://infra-postgres:5432` in production. The infrastructure topology is invisible to the application.
- **No secrets in images:** Environment variables (including LLM API keys) are injected at runtime via `.env` files or orchestrator configuration. Docker images are environment-agnostic.
- **Health check chain:** PostgreSQL and Redis report health via `pg_isready` / `redis-cli ping`. The API reports health via `GET /health`. Docker Compose enforces startup order via `depends_on` with `condition: service_healthy`.

## Development Workflow

- **Local development first:** Set up local development immediately after the base project structure exists (FastAPI app running, database connected, Alembic initialized, one module scaffolded). The application must start, serve the OpenAPI docs, and accept requests before adding feature code.
- **Dependency management:** Use `uv` (recommended) or `poetry` for Python dependency management. Pin all dependencies in `pyproject.toml`.
- **Type checking:** Use `mypy` or `pyright` in strict mode. Python's type hints combined with Pydantic provide strong type safety.
- **AI development loop:** Test LLM interactions with a lightweight provider (local Ollama or a cheap model) during development. Reserve expensive models for staging/production.

### Local Development Commands

```bash
# Start backing services (PostgreSQL with pgvector + Redis)
docker compose up -d

# Run migrations
uv run alembic upgrade head

# Start API with hot-reload
uv run uvicorn src.app.main:app --reload

# Start Celery worker (separate terminal)
uv run celery -A src.app.core.celery worker -l info

# Start frontend dev server (separate terminal, proxies /api to localhost:8000)
cd client && npm run dev
```

### Production Deployment

```bash
# Build images
docker compose -f docker-compose.prod.yml build

# Start application services (infra network must already exist)
docker compose -f docker-compose.prod.yml up -d

# Run migrations
docker exec <api-container> uv run alembic upgrade head

# Verify
curl http://localhost:<port>/health
```
