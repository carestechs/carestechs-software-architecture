# Service Layer Owns All Business Logic

**Category:** dotnet
**Stack:** dotnet
**Status:** Active
**Requires:** —
**Conflicts with:** `adrs/dotnet/cqrs-handlers.md`, `adrs/dotnet/rich-domain-entities.md`
**Last reviewed:** 2026-07-29

## Decision
All business logic lives in service classes. Controllers are thin: they validate input, call a service method, and return the result. Services are registered as scoped and injected via interfaces. No business logic is permitted in controllers or repository classes.

## Rationale
- Centralizing business logic in the service layer provides a single, testable location for domain rules. Controllers handle HTTP concerns; services handle business concerns. This separation makes unit testing straightforward — services can be tested without HTTP infrastructure.
- Alternatives considered: rich domain model with logic in entities (can complement services for entity-level invariants, but orchestration and cross-entity logic still belongs in services), MediatR handlers (adds indirection without clear benefit at current scale), CQRS with dedicated handlers (a valid alternative used by the Clean Architecture stack — see the conflicting `cqrs-handlers.md`; never mix the two patterns in one solution), logic in controllers (rejected — untestable without integration test infrastructure, mixes HTTP and domain concerns).
- Scoped lifetime aligns with the per-request DbContext lifetime, ensuring services and their DbContext share the same scope.

## Constraints (non-negotiable for AI)
- Controllers MUST only: parse/validate the request, call one or more service methods, and return an HTTP response.
- All business rules, validations beyond input format, orchestration, and data transformation MUST live in service classes.
- Services MUST be registered as scoped in DI.
- Services MUST be injected via their interface (e.g., `ICatalogService`), never as concrete classes.
- Service interfaces and implementations MUST live in the module's `Services/` folder.
- NEVER place business logic in repository classes — repositories (if used) are thin data-access wrappers only. In most cases, the DbContext itself is the repository.
