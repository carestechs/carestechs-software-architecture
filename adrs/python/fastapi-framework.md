# FastAPI as Web Framework

**Category:** python
**Stack:** python
**Status:** Active
**Requires:** —
**Conflicts with:** —
**Last reviewed:** 2026-07-29

## Decision
FastAPI is the web framework for all HTTP endpoints. It provides async-native request handling, automatic OpenAPI documentation, dependency injection, and Pydantic-based request/response validation out of the box.

## Rationale
- FastAPI is the industry standard for modern Python APIs. It combines high performance (Starlette/Uvicorn), automatic OpenAPI/Swagger docs, and native async support in a single framework.
- Alternatives considered: Django REST Framework (rejected — synchronous by default, heavier ORM coupling, slower for async workloads), Flask (rejected — validation and OpenAPI docs require third-party extensions, and its `async def` views run on WSGI without ASGI-level concurrency), Litestar (rejected — smaller community despite similar feature set).
- FastAPI's dependency injection system provides a clean way to compose services, database sessions, and auth dependencies without a separate DI container.
- Auto-generated OpenAPI docs mean the API is always self-documenting — no manual spec maintenance.

## Constraints (non-negotiable for AI)
- All HTTP endpoints MUST be defined as FastAPI route functions using `@router.get()`, `@router.post()`, etc.
- Every endpoint that accepts a request body MUST validate it with a Pydantic model, and every endpoint MUST declare a Pydantic `response_model` for serialization.
- FastAPI's dependency injection (`Depends()`) MUST be used for injecting services, database sessions, and auth dependencies into route functions.
- Route functions MUST be `async def` for any endpoint that performs I/O.
- Every router MUST be organized in its own module file and included via `app.include_router()`.
- NEVER define routes directly on the main `FastAPI()` app instance — always use `APIRouter`.
- The OpenAPI docs MUST remain enabled in development. Production may disable them via configuration.
