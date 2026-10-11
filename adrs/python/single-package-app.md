---
category: python
stack: python
family: single-deployable-app
status: Active
requires:
  - adrs/python/fastapi-framework.md
conflicts_with:
  - adrs/python/modular-packages.md
last_reviewed: 2026-10-11
---

# Single-Package Application

## Decision
The application is ONE Python package (`src/app/`) plus one test tree, organized as a folder per feature (`app/features/runs/`, `app/features/devices/`, each holding its router, service, models and schemas). One SQLAlchemy metadata and one Alembic history cover every table. There are no module packages, no contracts package, no per-module schemas. The service-layer and Pydantic-at-the-boundary rules apply in full; they are what keeps the later graduation to modular packages a restructuring PR.

## Rationale
- Modular packages with shared contracts, cross-module-by-id and a schema per module defend boundaries between people and domains. Below that threshold (one or two developers, one domain) the ceremony costs on every feature and defends nothing; the observed failure mode of premature modules is boundary rules that exist on paper and drift in practice.
- This is the Python first rung the catalog lacked: `modular-packages` starts at rung two, so Python projects began modular whether or not they had modules. The .NET side has had `single-project-monolith` for this since 2026-08.
- Alternatives considered: starting modular "because we will grow" (rejected; graduate when growth arrives, and feature folders become packages by `git mv` when the discipline held), one flat package organized by technical kind (rejected; a `routers/` folder with twenty routers is what makes a later extraction impossible), layer folders without feature boundaries (rejected; ceremony without the payoff).
- ORM relationships between features' models are permitted; there is no boundary to protect yet. The Pydantic boundary still keeps ORM models out of responses.

## Constraints (non-negotiable for AI)
- ONE application package and ONE test tree. NEVER add module packages, a `contracts/` package or a second Alembic history under this ADR; doing so is graduating to `modular-packages`, not bending this one.
- Features live in `app/features/<name>/` with their own `router.py`, `service.py`, `models.py` and `schemas.py`. NEVER organize by technical kind at the root.
- ONE `Base.metadata` and ONE Alembic `versions/` directory; every schema change ships as a migration in the same change set.
- Route handlers stay thin and services hold the logic; Pydantic schemas at every boundary. The `service-layer-logic` and `pydantic-at-boundary` rules apply unchanged.
- Relationships across features are allowed, but every feature's models stay in its own folder, so that extracting a feature later converts its relationships to plain ids (the `cross-module-by-id` rule) without a hunt.
- Graduate to `modular-packages` when two or more hold: more than ~3 developers stepping on each other, clearly distinguishable domains, onboarding pain, a feature that must evolve independently. Graduation is a restructuring PR, not a rewrite, if these constraints held.

## Examples

**Violation — organized by technical kind:**
```
src/app/routers/runs.py
src/app/routers/devices.py
src/app/services/runs.py
src/app/models/__init__.py      # every table of the system
```

**Compliant:**
```
src/app/features/runs/{router,service,models,schemas}.py
src/app/features/devices/{router,service,models,schemas}.py
src/app/core/database.py          # one Base, one engine
src/app/migrations/versions/      # one history
```
