# Stack Profile: Python + React Modular Monolith with AI Agent

**Status:** Active
**Assumes:** Python 3.12+, FastAPI 0.110+, React 19+, PostgreSQL 16+ (with pgvector), SQLAlchemy 2.0+, Tailwind CSS 4+, Vite 6+, Redis

## Overview

A curated set of ADRs for building a modular monolith backend with Python/FastAPI and a React SPA frontend, extended with AI agent capabilities. This stack builds on the base `Python + React Modular Monolith` profile and adds ADRs for LLM integration, tool calling, RAG, and conversation management. This is the industry-standard stack for AI-powered developer tools, research automation, and intelligent API products. ADRs are categorized by how essential they are to the stack's coherence.

---

## Solution Structure

```
myapp/
├── pyproject.toml                          # Project metadata, dependencies (uv/poetry)
├── alembic.ini                             # Alembic migration config
├── docker-compose.yml                      # PostgreSQL (pgvector), Redis, app services
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
│   ├── tailwind.config.ts
│   ├── vite.config.ts
│   └── package.json
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

| ADR | Summary | Depends On |
|-----|---------|------------|
| `adrs/python/fastapi-framework.md` | FastAPI as the web framework. Async-native, auto OpenAPI docs, Pydantic validation. | — |
| `adrs/python/modular-packages.md` | Feature modules as Python packages under `src/app/modules/`. Clear boundaries, shared contracts. | `fastapi-framework` |
| `adrs/python/service-layer-logic.md` | Route handlers are thin. All business logic in service functions/classes. | — |
| `adrs/python/pydantic-at-boundary.md` | Pydantic schemas for all request/response payloads. Never expose ORM models. | `service-layer-logic` |
| `adrs/python/async-all-the-way.md` | All I/O uses async/await. AsyncSession for DB. Uvicorn as ASGI server. | `fastapi-framework` |
| `adrs/python/sqlalchemy-async.md` | SQLAlchemy 2.0 async ORM with Alembic migrations. asyncpg driver. | `async-all-the-way` |
| `adrs/react/functional-components.md` | Functional components with hooks only. Feature-based folder organization. | — |
| `adrs/ai/ai-module-python.md` | AI agent is a dedicated module (`src/app/modules/ai/`) following all modular monolith conventions. | `modular-packages`, `service-layer-logic`, `pydantic-at-boundary`, `sqlalchemy-async` |
| `adrs/ai/llm-abstraction-python.md` | All LLM and embedding calls go through a provider-agnostic abstraction. Provider SDKs only in composition root. | `async-all-the-way`, `service-layer-logic` |
| `adrs/ai/tool-calling-via-services-python.md` | AI tools are thin adapters that delegate to existing service functions. No business logic in tools. | `modular-packages`, `service-layer-logic`, `llm-abstraction-python` |

## Recommended (strong defaults — can be swapped with noted alternatives)

These are battle-tested defaults. You can swap them, but you should have a good reason.

| ADR | Summary | Alternative |
|-----|---------|-------------|
| `adrs/python/celery-background-jobs.md` | Celery + Redis for background task processing. Tasks delegate to services. | ARQ or Dramatiq (smaller ecosystem) |
| `adrs/database/uuid-primary-keys.md` | All PKs are UUIDs. No auto-increment. | Auto-increment integers (simpler but less secure for external APIs) |
| `adrs/database/snake-case-naming.md` | snake_case tables/columns. Native to both Python and PostgreSQL. | — (already the natural convention) |
| `adrs/database/timestamptz-always.md` | All datetimes are timestamptz. Python uses `datetime` with UTC timezone. | timestamp without timezone (loses timezone context) |
| `adrs/api/rest-envelope.md` | All responses wrapped in `{ data, meta }` envelope. | Flat responses with pagination in headers |
| `adrs/api/jwt-bearer-auth.md` | JWT Bearer tokens. Short-lived access + rotated refresh. | API key auth (simpler for developer-facing APIs) |
| `adrs/react/tanstack-query.md` | TanStack Query for all server state. Caching, refetching, cache invalidation. | SWR (fewer features) or raw useEffect (not recommended) |
| `adrs/react/tailwind-shadcn.md` | Tailwind CSS + shadcn/ui components. Full ownership, no runtime dependency. | Material UI or Chakra UI (heavier, opinionated) |
| `adrs/ai/rag-pgvector-python.md` | RAG pipeline using pgvector for vector storage and cosine similarity search. | Dedicated vector DB (Pinecone, Qdrant) if scale demands it |
| `adrs/ai/conversation-history-python.md` | Multi-turn conversation persistence with token-aware context windowing. | Stateless single-turn interactions (if no conversation continuity needed) |

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
- **LLM provider independence:** Service code depends on abstract protocols. Swapping from OpenAI to Anthropic (or any other provider) is a composition root change only — no service code modifications.
- **AI tool boundary:** AI tools never contain business logic. They are thin adapters that delegate to the same service functions used by route handlers, ensuring consistent behavior whether triggered by HTTP or by LLM tool calling.
- **Vector storage colocation:** Embeddings live in PostgreSQL via pgvector alongside application data, avoiding a separate vector database deployment.
- **Context window safety:** Conversation history is always pruned before being sent to the LLM. Unbounded token usage is architecturally prevented.
- **RAG prompt hygiene:** Retrieved context is clearly delimited in prompts, chunk sizes are configurable, and all embeddings go through the provider-agnostic abstraction.
- **Background jobs:** Celery tasks (explorations, embedding generation, long-running AI operations) are thin wrappers that call the same service functions used by route handlers, ensuring consistent behavior.

## Development Workflow

- **Local development first:** Set up local development immediately after the base project structure exists (FastAPI app running, database connected, Alembic initialized, one module scaffolded). The application must start, serve the OpenAPI docs, and accept requests before adding feature code. Use Docker Compose for PostgreSQL (with pgvector) and Redis.
- **Dependency management:** Use `uv` (recommended) or `poetry` for Python dependency management. Pin all dependencies in `pyproject.toml`.
- **Type checking:** Use `mypy` or `pyright` in strict mode. Python's type hints combined with Pydantic provide strong type safety.
- **AI development loop:** Test LLM interactions with a lightweight provider (local Ollama or a cheap model) during development. Reserve expensive models for staging/production.
