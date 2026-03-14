# Service Layer Owns All Business Logic

**Category:** python
**Status:** Active
**Requires:** —
**Conflicts with:** —

## Decision
All business logic lives in service classes or functions within each module's `service.py`. Route handlers are thin: they validate input (via Pydantic), call a service function, and return the result. No business logic is permitted in route handlers or direct database queries within routes.

## Rationale
- Centralizing business logic in the service layer provides a single, testable location for domain rules. Route handlers handle HTTP concerns; services handle business concerns. Services can be tested without spinning up the ASGI server.
- Alternatives considered: logic in route handlers (rejected — untestable without full HTTP setup, mixes HTTP and domain concerns), repository pattern (rejected — SQLAlchemy sessions already serve as the data access layer), CQRS (rejected — adds complexity without clear benefit at current scale).
- Services receive their dependencies (database session, external clients) via function parameters or class constructor injection through FastAPI's `Depends()`.

## Constraints (non-negotiable for AI)
- Route handlers MUST only: extract/validate the request (Pydantic handles this), call one or more service functions, and return an HTTP response.
- All business rules, validations beyond input format, orchestration, and data transformation MUST live in service functions or classes.
- Services MUST be injected into route handlers via FastAPI's `Depends()` mechanism.
- Service functions that perform I/O MUST be `async def`.
- NEVER place business logic in route handlers — route handlers are thin wrappers only.
- NEVER perform raw database queries in route handlers — all data access goes through services.
