# Stack Profile: .NET + Angular Modular Monolith with AI Agent (Docker Compose)

**Status:** Active
**Assumes:** .NET 10+, Angular 20+, PostgreSQL (with pgvector), EF Core, Tailwind CSS 4+, Microsoft.Extensions.AI, Docker 24+, Docker Compose v2+

## Overview

A curated set of ADRs for building a modular monolith backend with an Angular SPA frontend, extended with AI agent capabilities, deployed via Docker Compose. This stack builds on the base `.NET + Angular Modular Monolith` profile and adds ADRs for LLM integration, tool calling, RAG, and conversation management. ADRs are categorized by how essential they are to the stack's coherence.

---

## Solution Structure

```
MyApp/
├── MyApp.sln
├── Dockerfile                              # Backend multi-stage build (dotnet/sdk → dotnet/aspnet)
├── .dockerignore                           # Build context exclusions
├── docker-compose.yml                      # Dev: PostgreSQL (pgvector) only
├── docker-compose.prod.yml                 # Prod: API + frontend on shared infra network
├── .env.example                            # Dev environment variable template
├── .env.production.example                 # Prod environment variable template
│
├── src/
│   ├── MyApp.Api/                          # Thin API host (composition root)
│   │   ├── Program.cs                      # DI registration, middleware, pipeline, LLM provider setup
│   │   ├── appsettings.json
│   │   └── MyApp.Api.csproj                # References all module projects + provider SDK packages
│   │
│   ├── MyApp.Contracts/                    # Shared interfaces and DTOs for cross-module communication
│   │   ├── ICatalogService.cs
│   │   ├── IIdentityService.cs
│   │   └── MyApp.Contracts.csproj
│   │
│   ├── MyApp.Modules.Catalog/             # Example feature module
│   │   ├── Controllers/
│   │   │   └── CatalogController.cs
│   │   ├── Services/
│   │   │   ├── ICatalogService.cs
│   │   │   └── CatalogService.cs
│   │   ├── Entities/
│   │   │   └── Product.cs
│   │   ├── DTOs/
│   │   │   ├── ProductDto.cs
│   │   │   └── CreateProductRequest.cs
│   │   ├── CatalogDbContext.cs
│   │   ├── CatalogModuleExtensions.cs      # AddCatalogModule()
│   │   └── MyApp.Modules.Catalog.csproj
│   │
│   ├── MyApp.Modules.Identity/            # Another feature module (same structure)
│   │   ├── Controllers/
│   │   ├── Services/
│   │   ├── Entities/
│   │   ├── DTOs/
│   │   ├── IdentityDbContext.cs
│   │   ├── IdentityModuleExtensions.cs     # AddIdentityModule()
│   │   └── MyApp.Modules.Identity.csproj
│   │
│   └── MyApp.Modules.AI/                  # AI agent module
│       ├── Controllers/
│       │   └── ChatController.cs
│       ├── Services/
│       │   ├── IChatOrchestrator.cs
│       │   └── ChatOrchestrator.cs
│       ├── Entities/
│       │   ├── Conversation.cs
│       │   ├── Message.cs
│       │   └── DocumentEmbedding.cs
│       ├── DTOs/
│       │   ├── ChatRequest.cs
│       │   ├── ChatResponse.cs
│       │   └── ConversationDto.cs
│       ├── Tools/                          # AI tool adapters (thin wrappers over service interfaces)
│       │   ├── CatalogSearchTool.cs
│       │   └── OrderLookupTool.cs
│       ├── AIDbContext.cs
│       ├── AIModuleExtensions.cs           # AddAIModule()
│       └── MyApp.Modules.AI.csproj         # References Microsoft.Extensions.AI.Abstractions only
│
├── client/                                 # Angular SPA
│   ├── src/
│   │   ├── app/
│   │   │   ├── core/                       # Singleton services, guards, interceptors
│   │   │   ├── shared/                     # Reusable standalone components, pipes, directives
│   │   │   ├── features/                   # Feature-based route folders
│   │   │   │   ├── catalog/
│   │   │   │   │   ├── catalog.routes.ts
│   │   │   │   │   ├── catalog-list.component.ts
│   │   │   │   │   ├── catalog-list.component.html
│   │   │   │   │   ├── catalog-detail.component.ts
│   │   │   │   │   └── catalog-detail.component.html
│   │   │   │   ├── chat/                   # AI chat feature
│   │   │   │   │   ├── chat.routes.ts
│   │   │   │   │   ├── chat.component.ts
│   │   │   │   │   ├── chat.component.html
│   │   │   │   │   ├── chat-history.component.ts
│   │   │   │   │   └── chat-history.component.html
│   │   │   │   └── auth/
│   │   │   │       ├── auth.routes.ts
│   │   │   │       ├── login.component.ts
│   │   │   │       └── login.component.html
│   │   │   ├── app.component.ts
│   │   │   ├── app.component.html
│   │   │   ├── app.config.ts
│   │   │   └── app.routes.ts
│   │   ├── styles.css                      # Global Tailwind imports only
│   │   └── index.html
│   ├── Dockerfile                           # Multi-stage: Node build → nginx
│   ├── nginx.conf                           # SPA serving + API reverse proxy
│   ├── tailwind.config.js
│   ├── angular.json
│   └── package.json
│
├── scripts/
│   └── verify-docker.sh                     # Deployment smoke test
│
└── tests/
    ├── MyApp.Modules.Catalog.Tests/
    ├── MyApp.Modules.Identity.Tests/
    └── MyApp.Modules.AI.Tests/
```

---

## Required (core to this stack — do not omit)

These ADRs define the fundamental architecture. Removing any of them breaks the coherence of the stack.

| ADR | Summary | Depends On |
|-----|---------|------------|
| `adrs/dotnet/modular-monolith.md` | Single deployable, feature modules as separate .csproj with clear boundaries | — |
| `adrs/dotnet/dbcontext-per-module.md` | Each module owns its own DbContext. Migrations are per-module. | `modular-monolith` |
| `adrs/dotnet/cross-module-by-id.md` | Modules reference each other by ID only. No cross-module navigation properties. | `modular-monolith`, `dbcontext-per-module` |
| `adrs/dotnet/thin-api-host.md` | API host is composition root only — no controllers, services, or business logic. | `modular-monolith` |
| `adrs/dotnet/service-layer-logic.md` | Controllers are thin. All business logic lives in service classes. | — |
| `adrs/dotnet/dto-at-boundary.md` | Never expose EF entities via API. Mapping happens in service layer. | `service-layer-logic` |
| `adrs/dotnet/async-all-the-way.md` | All I/O uses async/await. Async suffix on service methods. | — |
| `adrs/angular/standalone-components.md` | All components standalone. No NgModules. | — |
| `adrs/ai/ai-agent-module.md` | AI agent is a dedicated module (`MyApp.Modules.AI`) following all modular monolith conventions. | `modular-monolith`, `dbcontext-per-module`, `cross-module-by-id`, `thin-api-host`, `service-layer-logic`, `dto-at-boundary` |
| `adrs/ai/meai-abstraction.md` | All LLM and embedding calls go through M.E.AI abstractions (`IChatClient`, `IEmbeddingGenerator`). | `modular-monolith`, `async-all-the-way` |
| `adrs/ai/tool-calling-via-services.md` | AI tools are thin adapters that delegate to existing service interfaces. No business logic in tools. | `modular-monolith`, `service-layer-logic`, `meai-abstraction` |
| `adrs/deployment/docker-multi-stage-builds.md` | All components packaged as Docker images with multi-stage builds. `dotnet/aspnet` final stage for backend. | — |
| `adrs/deployment/env-connection-urls.md` | All config via env vars. External services via connection URLs. Strongly-typed `IConfiguration` sections validate at startup. | — |
| `adrs/deployment/container-per-process.md` | API and frontend as separate containers. | `docker-multi-stage-builds` |
| `adrs/deployment/local-dev-compose.md` | `docker-compose.yml` for local infra, `docker-compose.prod.yml` for app services on shared network. | `docker-multi-stage-builds`, `env-connection-urls` |

## Recommended (strong defaults — can be swapped with noted alternatives)

These are battle-tested defaults. You can swap them, but you should have a good reason.

| ADR | Summary | Alternative |
|-----|---------|-------------|
| `adrs/dotnet/rfc7807-errors.md` | RFC 7807 Problem Details for all errors. Global exception handler. | Custom error envelope (not recommended) |
| `adrs/dotnet/xunit-per-module-tests.md` | xUnit test projects mirroring modules/layers. Real PostgreSQL (Testcontainers) for data-access tests. | NUnit (viable alternative) |
| `adrs/dotnet/structured-logging.md` | ILogger<T> with message templates. JSON output + correlation IDs in production. | Serilog as host provider (compatible) |
| `adrs/deployment/github-actions-ci.md` | CI gate on every push/PR: lint (enforcement configs), typecheck, tests, build. Deploys are separate workflows. | GitLab CI / Azure DevOps (if repos move) |
| `adrs/database/uuid-primary-keys.md` | All PKs are UUIDs. No auto-increment. | Auto-increment integers (simpler but less secure) |
| `adrs/database/snake-case-naming.md` | snake_case tables/columns via EF Core naming convention. | PascalCase with quoting (non-idiomatic for PostgreSQL) |
| `adrs/database/timestamptz-always.md` | All datetimes are timestamptz. C# uses DateTimeOffset. | timestamp without timezone (loses timezone context) |
| `adrs/api/rest-envelope.md` | All responses wrapped in `{ data, meta }` envelope. | Flat responses with pagination in headers |
| `adrs/api/jwt-bearer-auth.md` | JWT Bearer tokens. Short-lived access + rotated refresh. | Session cookies (if not SPA architecture) |
| `adrs/angular/separate-template-file.md` | Component templates in separate `.html` files via `templateUrl`. No inline templates. | Inline `template` strings (loses HTML tooling and readability) |
| `adrs/angular/signals-state.md` | Angular Signals for reactive state. RxJS only for HTTP/async. | RxJS BehaviorSubjects (more boilerplate) |
| `adrs/angular/tailwind-no-css.md` | Tailwind utility classes only. No component CSS files. | Component-scoped SCSS (if team prefers) |
| `adrs/ai/rag-pgvector.md` | RAG pipeline using pgvector for vector storage and similarity search. | Dedicated vector DB (Pinecone, Qdrant) if scale demands it |
| `adrs/ai/conversation-history.md` | Multi-turn conversation persistence with token-aware context windowing. | Stateless single-turn interactions (if no conversation continuity needed) |
| `adrs/deployment/nginx-spa-proxy.md` | Nginx serves built SPA and reverse-proxies `/api/` to backend. `try_files` for client-side routing. | Serving SPA from backend framework or separate CDN |

## Optional (pick based on project needs)

These address specific concerns that not every project has.

| ADR | Summary | When to Include |
|-----|---------|-----------------|
| `adrs/database/soft-deletes.md` | Soft deletion via nullable `deleted_at` column. | Projects needing audit trails or undo capability |
| `adrs/api/offset-pagination.md` | Offset pagination with page/pageSize/sortBy/sortDir. Requires `rest-envelope`. | Any project with list endpoints |
| `adrs/api/role-based-authorization.md` | Role gates at the endpoint layer + ownership checks in services. Deny by default. Requires `jwt-bearer-auth`. | Policy engine (OPA/Casbin) at larger scale |

---

## Key Cross-Cutting Concerns

When using this stack, these patterns emerge from the combination of ADRs:

- **Naming translation:** C# PascalCase properties automatically map to snake_case database columns and camelCase JSON (via System.Text.Json default policy)
- **Time handling:** Backend stores UTC DateTimeOffset, database uses timestamptz, frontend converts to local display time
- **ID strategy:** UUIDs flow end-to-end: generated in C#, stored as uuid in PostgreSQL, serialized as strings in JSON
- **Auth flow:** Angular app stores JWT in memory or httpOnly cookie, sends via Authorization header, .NET validates with `[Authorize]`
- **Module isolation:** Each module is a .csproj with its own DbContext, controllers, services, and DTOs. Cross-module communication is by ID + shared interface only.
- **LLM provider independence:** Service code depends only on `IChatClient` and `IEmbeddingGenerator` from M.E.AI. Swapping from OpenAI to Azure OpenAI (or any other provider) is a composition root change only.
- **AI tool boundary:** AI tools never contain business logic. They are thin adapters that delegate to the same service interfaces used by REST controllers, ensuring consistent behavior.
- **Vector storage colocation:** Embeddings live in PostgreSQL via pgvector alongside application data, avoiding a separate vector database deployment.
- **Context window safety:** Conversation history is always pruned or summarized before being sent to the LLM. Unbounded token usage is architecturally prevented.
- **RAG prompt hygiene:** Retrieved context is clearly delimited in prompts, chunk sizes are configurable, and all embeddings go through the M.E.AI abstraction.
- **Environment parity via connection URLs:** The same application code connects to `localhost:5432` in development and `infra-postgres:5432` in production. The infrastructure topology is invisible to the application.
- **No secrets in images:** Environment variables (including LLM API keys) are injected at runtime via `.env` files or orchestrator configuration. Docker images are environment-agnostic.
- **Health check chain:** PostgreSQL reports health via `pg_isready`. The API reports health via `GET /health`. Docker Compose enforces startup order via `depends_on` with `condition: service_healthy`.

## Development Workflow

- **Local development first:** Set up local development immediately after the base projects have minimal setup (solution structure, project references, empty DbContexts, and module registration wired in `Program.cs`). The application must build, run, and be locally testable before adding any feature code. This ensures a fast feedback loop and catches configuration issues early — never defer local dev setup to "later".

### Local Development Commands

```bash
# Start backing services (PostgreSQL with pgvector)
docker compose up -d

# Run migrations
dotnet ef database update --project src/MyApp.Modules.Catalog

# Start API with hot-reload
dotnet run --project src/MyApp.Api

# Start frontend dev server (separate terminal, proxies /api to localhost:5000)
cd client && ng serve --proxy-config proxy.conf.json
```

### Production Deployment

```bash
# Build images
docker compose -f docker-compose.prod.yml build

# Start application services (infra network must already exist)
docker compose -f docker-compose.prod.yml up -d

# Run migrations
docker exec <api-container> dotnet ef database update

# Verify
curl http://localhost:<port>/health
```
