---
category: python
stack: python
status: Active
requires:
  - adrs/python/fastapi-framework.md
conflicts_with:
  - adrs/dotnet/modular-monolith.md
last_reviewed: 2026-07-29
---

# Modular Monolith via Python Packages

## Decision
The system is built as a modular monolith: a single deployable unit composed of feature modules, each implemented as a Python package with clear boundaries. Each module owns its own routers, services, models, schemas, and clearly attributed Alembic migrations. Modules communicate through explicit interfaces, never through direct model imports across boundaries.

## Rationale
- A modular monolith gives the organizational benefits of microservices (bounded contexts, team ownership, independent evolution) without the operational complexity of distributed systems. Each module is a Python package with enforced import boundaries.
- Alternatives considered: flat Flask/FastAPI structure (rejected — leads to spaghetti imports over time), microservices (rejected — premature for current scale; can extract modules later), Django apps (rejected — too tightly coupled to Django's ORM and admin).
- Python packages with `__init__.py` provide a natural module boundary. Linting tools (import-linter, ruff) can enforce that modules do not cross-import.
- This architecture supports future extraction: any module can be promoted to an independent service by replacing its in-process function calls with HTTP/gRPC calls.

## Constraints (non-negotiable for AI)
- Every feature module MUST be its own Python package under `src/app/modules/<module_name>/`.
- A module MUST contain: `router.py` (API routes), `service.py` (business logic), `models.py` (SQLAlchemy entities), `schemas.py` (Pydantic DTOs), and `dependencies.py` (FastAPI dependencies).
- Modules MUST NOT import models or services from other module packages directly. Cross-module communication goes through interfaces defined in a shared `src/app/contracts/` package.
- The application entrypoint (`src/app/main.py`) is the only place that imports and registers module routers.
- No circular dependencies between modules. If two modules need each other, extract the shared concept into the contracts package.
- Each module MUST expose a `create_router()` function or a router instance for registration in the main app.
- Alembic migrations live in the shared `src/app/migrations/` directory with a single linear history; each migration's slug MUST be prefixed with the owning module's name (e.g., `<rev>_catalog_add_products.py`).

## Examples

**Violation — direct import across a module boundary:**
```python
# src/app/modules/catalog/service.py
from app.modules.identity.models import User  # reaches into another module
```

**Compliant:**
```python
# src/app/modules/catalog/service.py
from app.contracts.identity import IdentityService  # shared contract only

async def get_owner_email(identity: IdentityService, owner_id: UUID) -> str:
    return (await identity.get_user(owner_id)).email
```
