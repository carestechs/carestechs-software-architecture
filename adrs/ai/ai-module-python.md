---
category: ai
stack: python
status: Active
requires:
  - adrs/python/modular-packages.md
  - adrs/python/service-layer-logic.md
  - adrs/python/pydantic-at-boundary.md
  - adrs/python/sqlalchemy-async.md
conflicts_with:
  - adrs/ai/ai-agent-module.md
last_reviewed: 2026-07-29
---

# AI Agent as a Dedicated Python Module

## Decision
The AI agent is a dedicated feature module (`src/app/modules/ai/`) that follows all modular monolith conventions: its own package directory, its own SQLAlchemy models, its own routers/services/schemas, and clearly attributed Alembic migrations. The AI module accesses other modules exclusively through shared contract interfaces.

## Rationale
- Treating the AI agent as a first-class module ensures it respects the same boundaries, data ownership, and isolation rules as every other module. This prevents AI concerns from leaking into business modules and keeps the AI surface area auditable.
- Alternatives considered: embedding AI logic in a shared utility package (rejected — violates module ownership and makes it impossible to evolve AI independently), a separate microservice (rejected — premature for current scale; the module can be extracted later), scattering AI endpoints across existing modules (rejected — AI orchestration logic has its own lifecycle and dependencies).
- The AI module owns its own tables (conversations, messages, embeddings) as SQLAlchemy models, keeping AI-specific data out of business module schemas.
- Cross-module references use plain UUID values rather than SQLAlchemy relationships, consistent with modular boundaries.

## Constraints (non-negotiable for AI)
- The AI module MUST live in `src/app/modules/ai/` as a Python package.
- The AI module MUST contain: `router.py`, `service.py`, `models.py`, `schemas.py`, `dependencies.py`, and a `tools/` sub-package.
- The AI module MUST define its own SQLAlchemy models for conversations, messages, and embeddings.
- Cross-module references MUST be plain UUID values — no SQLAlchemy relationships to models owned by other modules.
- The AI module MUST NOT contain business logic belonging to other domains. It delegates to other modules via shared contract interfaces in `src/app/contracts/`.
- The AI module MUST expose a router for registration in the main app.
- Migrations for AI-owned tables MUST use the module-prefix convention: slug prefixed with `ai_` (e.g., `<rev>_ai_add_conversations.py`) in the shared migration history.
