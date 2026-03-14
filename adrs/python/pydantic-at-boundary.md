# Pydantic Schemas at API Boundary

**Category:** python
**Status:** Active
**Requires:** `adrs/python/service-layer-logic.md`
**Conflicts with:** —

## Decision
The API never exposes SQLAlchemy models directly. All request and response payloads use dedicated Pydantic schemas. Mapping between SQLAlchemy models and Pydantic schemas happens in the service layer. Schemas are defined in each module's `schemas.py` file.

## Rationale
- Exposing ORM models directly couples the API contract to the database schema, making both harder to evolve independently. Pydantic schemas provide a stable, validated API surface regardless of how the database schema changes.
- Alternatives considered: returning ORM models directly with `from_attributes=True` (rejected — leaks internal fields, lacks explicit control over serialization), dataclasses (rejected — no built-in validation, no JSON schema generation), marshmallow (rejected — Pydantic is natively integrated with FastAPI).
- Pydantic v2's `model_config = ConfigDict(from_attributes=True)` enables seamless mapping from SQLAlchemy models while still requiring explicit schema definitions.

## Constraints (non-negotiable for AI)
- Route handlers MUST use Pydantic schemas for all request bodies and response models.
- NEVER return SQLAlchemy model instances directly from route handlers or services that feed into responses.
- Each module MUST define its schemas in `schemas.py` within the module package.
- Request schemas and response schemas MUST be separate classes (e.g., `CreateProductRequest`, `ProductResponse`). NEVER reuse the same schema for input and output.
- Schemas MUST use `model_config = ConfigDict(from_attributes=True)` when they need to be constructed from ORM model instances.
- Mapping from ORM models to Pydantic schemas MUST happen in the service layer, not in route handlers.
