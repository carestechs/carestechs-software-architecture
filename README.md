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
│   │   ├── rfc7807-errors.md
│   │   ├── clean-architecture-layers.md
│   │   ├── cqrs-handlers.md
│   │   ├── rich-domain-entities.md
│   │   ├── result-pattern-errors.md
│   │   ├── event-driven-reactors.md
│   │   ├── xunit-per-module-tests.md
│   │   └── structured-logging.md
│   │
│   ├── python/                    # Python / FastAPI decisions
│   │   ├── fastapi-framework.md
│   │   ├── modular-packages.md
│   │   ├── service-layer-logic.md
│   │   ├── pydantic-at-boundary.md
│   │   ├── async-all-the-way.md
│   │   ├── sqlalchemy-async.md
│   │   ├── celery-background-jobs.md
│   │   ├── rfc7807-errors.md
│   │   ├── pytest-testing.md
│   │   └── structured-logging.md
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
│   ├── typescript/                # TypeScript / Node.js decisions
│   │   ├── strict-typescript.md
│   │   ├── named-exports.md
│   │   ├── functional-composition.md
│   │   ├── types-at-boundary.md
│   │   ├── core-adapter-pattern.md
│   │   └── vitest-colocated.md
│   │
│   ├── database/                  # Database design decisions
│   │   ├── uuid-primary-keys.md
│   │   ├── snake-case-naming.md
│   │   ├── lowercase-naming.md
│   │   ├── soft-deletes.md
│   │   └── timestamptz-always.md
│   │
│   ├── api/                       # API design decisions
│   │   ├── rest-envelope.md
│   │   ├── jwt-bearer-auth.md
│   │   ├── role-based-authorization.md
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
│   │   ├── conversation-history-python.md       # Python variant
│   │   └── claude-agent-sdk.md          # TypeScript (direct SDK, no abstraction)
│   │
│   └── deployment/                # Containerization, config, and infrastructure
│       ├── docker-multi-stage-builds.md
│       ├── env-connection-urls.md
│       ├── container-per-process.md
│       ├── local-dev-compose.md
│       ├── nginx-spa-proxy.md
│       ├── aws-lambda-serverless.md
│       ├── aws-sam-infrastructure.md
│       ├── aws-secrets-parameters.md
│       ├── flyway-migrations.md
│       ├── queue-based-decoupling.md
│       ├── tauri-desktop-shell.md
│       ├── aws-batch-workers.md
│       ├── maintenance-cli-scheduler.md
│       ├── npm-cli-package.md
│       ├── zod-config-validation.md
│       ├── github-action-composite.md
│       └── github-actions-ci.md
│
└── profiles/                  # Pre-built ADR sets (stack + deployment mode)
    ├── dotnet-angular-modular-monolith-docker-compose.md
    ├── dotnet-angular-ai-agent-docker-compose.md
    ├── python-react-modular-monolith-docker-compose.md
    ├── python-react-ai-agent-docker-compose.md
    ├── dotnet-angular-clean-architecture-aws-lambda.md
    ├── typescript-cli-tool-npm.md
    └── typescript-ai-agent-cli-npm.md
```

**75 ADRs** across 9 categories, **7 stack profiles**.

## How to Use

### 1. Pick a Profile (or Select Individual ADRs)

Profiles are curated sets of ADRs organized into Required, Recommended, and Optional tiers. Each profile covers the full stack: application architecture, deployment mode, and workflow. Start with the profile closest to your tech stack:

| Profile | Stack | Deploy Mode |
|---------|-------|-------------|
| **python-react-modular-monolith-docker-compose** | Python + React + PostgreSQL | Docker Compose |
| **python-react-ai-agent-docker-compose** | Python + React + PostgreSQL + AI | Docker Compose |
| **dotnet-angular-modular-monolith-docker-compose** | .NET + Angular + PostgreSQL | Docker Compose |
| **dotnet-angular-ai-agent-docker-compose** | .NET + Angular + PostgreSQL + AI | Docker Compose |
| **dotnet-angular-clean-architecture-aws-lambda** | .NET + Angular + PostgreSQL | AWS Lambda |
| **typescript-cli-tool-npm** | TypeScript CLI / dev tool | npm |
| **typescript-ai-agent-cli-npm** | TypeScript CLI + Claude Agent SDK | npm |

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
| dotnet | 15 | Modular monolith, Clean Architecture, CQRS, rich entities, Result pattern, events, DbContext, DTOs, async, xUnit testing, structured logging |
| python | 10 | FastAPI, modular packages, Pydantic, SQLAlchemy, Celery, Problem Details errors, pytest testing, structured logging |
| angular | 4 | Standalone components, templates, Signals, Tailwind |
| react | 3 | Functional components, TanStack Query, Tailwind + shadcn |
| typescript | 6 | Strict TS, named exports, functional composition, boundary types, core/adapter, Vitest |
| database | 5 | UUID PKs, snake_case naming, lowercase naming, soft deletes, timestamptz |
| api | 4 | REST envelope, JWT auth, role-based authorization, offset pagination |
| ai | 11 | AI agent modules, LLM abstraction, tool calling, RAG, conversation history, Claude Agent SDK |
| deployment | 17 | Docker builds, env config, containers, Compose, nginx, AWS Lambda, SAM/CloudFormation, AWS Batch, Flyway, SQS queues, Secrets Manager, Tauri desktop, npm CLI packaging, GitHub Actions CI + composite action, Zod config, maintenance scheduler |

## ADR Format

Every ADR follows this standard format (see `ADR-FORMAT.md` for the full template):

```markdown
---
category: dotnet | python | typescript | angular | react | database | api | ai | deployment
stack: dotnet | python | typescript | angular | react | any
status: Active
requires: []          # ADR dependencies; " | " inside an item separates alternatives
conflicts_with: []    # mutually exclusive ADRs — always declared symmetrically
last_reviewed: YYYY-MM-DD
---

# [Decision Title]

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

- **Requires:** Lists ADR files that must also be selected. A required ADR being missing indicates an incomplete set. An entry may list alternatives separated by ` | ` — at least one of them must be selected (e.g., `adrs/dotnet/modular-monolith.md` | `adrs/dotnet/clean-architecture-layers.md`).
- **Conflicts with:** Lists ADR files that are mutually exclusive. Conflicting ADRs should not both be selected. Conflicts are declared symmetrically: if A lists B, B lists A.
- **Stack:** Which technology stack the ADR's constraints assume (`any` = cross-stack). Same-slot variants for different stacks conflict with each other; filter by Stack to find the variant that applies to your project.

Run `python scripts/validate_adrs.py` to check the catalog: frontmatter format, referenced files, conflict symmetry, dependency cycles, and profile consistency. CI runs the same script on every push and pull request. `python scripts/validate_adrs.py --stale` additionally reports version-sensitive ADRs (those with a `verify_against` list) that are due for re-review.

Example: `adrs/dotnet/dbcontext-per-module.md` requires `adrs/dotnet/modular-monolith.md` — you can't have per-module DbContexts without module boundaries.

## Enforcement

Machine-checkable constraints ship as ready-to-copy lint and analyzer configurations in `enforcement/` — BannedApiAnalyzers + `.editorconfig` rules for .NET, a `ruff.toml` for Python, and an ESLint flat config + base `tsconfig` for TypeScript, each annotated with the ADR it enforces. See `enforcement/README.md` for the constraint-to-rule mapping and adoption instructions. Constraints without a rule remain prompt-only (compiled CLAUDE.md + review).

## Versioning

Catalog releases are tagged `vMAJOR.MINOR.PATCH`:

- **MAJOR** — metadata-format or semantic changes that break consumers (compile prompts, validators, tooling)
- **MINOR** — new ADRs, new constraints, or profile changes (agents may start doing something new)
- **PATCH** — clarifications and corrections that do not change what an agent should do

When compiling ADRs into project documentation, record the tag in the output (e.g., `Compiled from carestechs-software-architecture@v1.0.0`). That makes drift between a project's rules and the current catalog visible and diffable. `v1.0.0` is the first fully validated catalog (symmetric conflict graph, YAML frontmatter, CI).

## Relationship to Companion Repos

This repo is part of the **carestechs** ecosystem of reusable project scaffolding:

```
carestechs-software-architecture/   → ADRs (how to build)
carestechs-ui-design/               → DDRs (how things look)
carestechs-ia-framework/            → Templates, prompts, guides (how to use them together)
```

The `carestechs-ia-framework` provides a `compile-adrs.md` prompt that reads ADRs from this repo and derives pre-filled template sections for project documentation (ARCHITECTURE.md, CLAUDE.md, data-model.md, api-spec.md).
