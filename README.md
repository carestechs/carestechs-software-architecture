# Architecture Decision Records

A reusable library of architectural decisions extracted from real projects.

## What This Is

This repo contains **stack-specific architectural decisions** documented as ADRs (Architecture Decision Records). Each ADR captures:

- **Decision** — what was decided (1-2 sentences)
- **Rationale** — why, and what alternatives were considered
- **Constraints** — hard rules that must never be violated

## Why It Exists

When you've already made strong architectural decisions — modular monolith, DbContext-per-module, UUID primary keys, etc. — you want to **reuse those decisions across projects** without re-documenting them every time.

This repo is the source of truth for reusable decisions. Tools and frameworks can read these ADRs and incorporate them into project-specific documentation.

## Directory Structure

```
carestechs-software-architecture/
├── README.md
├── ADR-FORMAT.md              # Template for creating new ADRs
├── CONTRIBUTING.md            # Guidelines for adding new ADRs and profiles
│
├── adrs/
│   ├── dotnet/                    # .NET / C# decisions
│   │   ├── modular-monolith.md
│   │   ├── dbcontext-per-module.md
│   │   ├── cross-module-by-id.md
│   │   ├── thin-api-host.md
│   │   ├── service-layer-logic.md
│   │   ├── dto-at-boundary.md
│   │   ├── async-all-the-way.md
│   │   └── rfc7807-errors.md
│   │
│   ├── python/                    # Python / FastAPI decisions
│   │   ├── fastapi-framework.md
│   │   ├── modular-packages.md
│   │   ├── service-layer-logic.md
│   │   ├── pydantic-at-boundary.md
│   │   ├── async-all-the-way.md
│   │   ├── sqlalchemy-async.md
│   │   └── celery-background-jobs.md
│   │
│   ├── angular/                   # Angular decisions
│   │   ├── standalone-components.md
│   │   ├── separate-template-file.md
│   │   ├── signals-state.md
│   │   └── tailwind-no-css.md
│   │
│   ├── react/                     # React decisions
│   │   ├── functional-components.md
│   │   ├── tanstack-query.md
│   │   └── tailwind-shadcn.md
│   │
│   ├── database/                  # Database design decisions
│   │   ├── uuid-primary-keys.md
│   │   ├── snake-case-naming.md
│   │   ├── soft-deletes.md
│   │   └── timestamptz-always.md
│   │
│   ├── api/                       # API design decisions
│   │   ├── rest-envelope.md
│   │   ├── jwt-bearer-auth.md
│   │   └── offset-pagination.md
│   │
│   ├── ai/                        # AI agent decisions
│   │   ├── ai-agent-module.md           # .NET variant
│   │   ├── ai-module-python.md          # Python variant
│   │   ├── meai-abstraction.md          # .NET (M.E.AI)
│   │   ├── llm-abstraction-python.md    # Python (provider-agnostic)
│   │   ├── tool-calling-via-services.md         # .NET variant
│   │   ├── tool-calling-via-services-python.md  # Python variant
│   │   ├── rag-pgvector.md              # .NET variant
│   │   ├── rag-pgvector-python.md       # Python variant
│   │   ├── conversation-history.md      # .NET variant
│   │   └── conversation-history-python.md       # Python variant
│   │
│   └── deployment/                # Containerization, config, and infrastructure
│       ├── docker-multi-stage-builds.md
│       ├── env-connection-urls.md
│       ├── container-per-process.md
│       ├── local-dev-compose.md
│       └── nginx-spa-proxy.md
│
└── profiles/                  # Pre-built ADR sets (stack + deployment mode)
    ├── dotnet-angular-modular-monolith-docker-compose.md
    ├── dotnet-angular-ai-agent-docker-compose.md
    ├── python-react-modular-monolith-docker-compose.md
    └── python-react-ai-agent-docker-compose.md
```

**47 ADRs** across 8 categories, **4 stack profiles**.

## How to Use

### 1. Pick a Profile (or Select Individual ADRs)

Profiles are curated sets of ADRs organized into Required, Recommended, and Optional tiers. Each profile covers the full stack: application architecture, deployment mode, and workflow. Start with the profile closest to your tech stack:

| Profile | Stack | Deploy Mode |
|---------|-------|-------------|
| **python-react-modular-monolith-docker-compose** | Python + React + PostgreSQL | Docker Compose |
| **python-react-ai-agent-docker-compose** | Python + React + PostgreSQL + AI | Docker Compose |
| **dotnet-angular-modular-monolith-docker-compose** | .NET + Angular + PostgreSQL | Docker Compose |
| **dotnet-angular-ai-agent-docker-compose** | .NET + Angular + PostgreSQL + AI | Docker Compose |

Or select individual ADRs from any category to build a custom set.

### 2. Compile ADRs into Template Sections

Use the `compile-adrs.md` prompt from the `carestechs-ia-framework`:

**AI agents:** Read the ADR files and the profile, then apply the derivation rules to generate pre-filled template sections.

**Chat workflows:** Paste ADR contents and the profile into the XML template in `compile-adrs.md`.

### 3. Paste into Project Templates

The compilation output fills:
- **ARCHITECTURE.md** — System structure, module boundaries, cross-cutting concerns
- **CLAUDE.md** — Code conventions, patterns to follow/avoid
- **data-model.md** — Entity conventions, naming, types
- **api-spec.md** — API design conventions, auth, pagination

### 4. Fill in Project-Specific Content

After compiling, you still need to fill in:
- Module inventory and module-specific details
- Entity definitions, fields, and relationships
- API endpoints and DTOs
- Project-specific architectural decisions

## Categories

| Category | Count | What It Covers |
|----------|-------|----------------|
| dotnet | 8 | Modular monolith, DbContext, services, DTOs, async, error handling |
| python | 7 | FastAPI, modular packages, Pydantic, SQLAlchemy, Celery |
| angular | 4 | Standalone components, templates, Signals, Tailwind |
| react | 3 | Functional components, TanStack Query, Tailwind + shadcn |
| database | 4 | UUID PKs, snake_case naming, soft deletes, timestamptz |
| api | 3 | REST envelope, JWT auth, offset pagination |
| ai | 10 | AI agent modules, LLM abstraction, tool calling, RAG, conversation history |
| deployment | 5 | Docker multi-stage builds, env-based config, container-per-process, dev/prod Compose, nginx SPA proxy |

## ADR Format

Every ADR follows this standard format (see `ADR-FORMAT.md` for the full template):

```markdown
# [Decision Title]

**Category:** dotnet | python | angular | react | database | api | ai | deployment
**Status:** Active
**Requires:** [ADR dependencies — file paths, or omit if none]
**Conflicts with:** [Mutually exclusive ADRs — file paths, or omit if none]

## Decision
[What was decided — 1-2 sentences]

## Rationale
- [Why this decision was made]
- [What alternatives were considered]

## Constraints (non-negotiable for AI)
- [Hard rule 1 — the AI must never violate this]
- [Hard rule 2]
```

## ADR Dependencies

ADRs can declare dependencies and conflicts in their metadata:

- **Requires:** Lists ADR files that must also be selected. A required ADR being missing indicates an incomplete set.
- **Conflicts with:** Lists ADR files that are mutually exclusive. Conflicting ADRs should not both be selected.

Example: `adrs/dotnet/dbcontext-per-module.md` requires `adrs/dotnet/modular-monolith.md` — you can't have per-module DbContexts without module boundaries.

## Relationship to Companion Repos

This repo is part of the **carestechs** ecosystem of reusable project scaffolding:

```
carestechs-software-architecture/   → ADRs (how to build)
carestechs-ui-design/               → DDRs (how things look)
carestechs-ia-framework/            → Templates, prompts, guides (how to use them together)
```

The `carestechs-ia-framework` provides a `compile-adrs.md` prompt that reads ADRs from this repo and derives pre-filled template sections for project documentation (ARCHITECTURE.md, CLAUDE.md, data-model.md, api-spec.md).
