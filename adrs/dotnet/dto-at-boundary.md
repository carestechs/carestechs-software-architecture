# DTOs at the API Boundary

**Category:** dotnet
**Status:** Active
**Requires:** `adrs/dotnet/service-layer-logic.md`
**Conflicts with:** —

## Decision
API controllers never expose EF Core entities directly. Every endpoint uses dedicated request and response DTOs. Mapping between entities and DTOs happens in the service layer. DTOs live in each module's `DTOs/` folder.

## Rationale
- Exposing EF entities through API endpoints creates tight coupling between the database schema and the API contract. Any schema change (column rename, new relationship, field removal) would be a breaking API change.
- Alternatives considered: AutoMapper for mapping (acceptable but not required — manual mapping is preferred for clarity and debuggability), shared DTOs across modules (rejected — each module owns its own DTOs), JsonIgnore attributes on entities (rejected — fragile, easy to forget, mixes concerns).
- DTOs provide a clear contract: request DTOs define what the API accepts, response DTOs define what the API returns. This makes API documentation accurate and versioning possible.
- Mapping in the service layer (rather than the controller) keeps controllers thin and ensures the mapping logic is testable.

## Constraints (non-negotiable for AI)
- NEVER return an EF entity directly from a controller action.
- NEVER accept an EF entity as a parameter in a controller action.
- Every API endpoint MUST use request DTOs for input and response DTOs for output.
- DTOs MUST be placed in the module's `DTOs/` folder.
- Mapping between entities and DTOs MUST happen in the service layer, not in controllers.
- DTOs MUST be simple POCOs — no business logic, no methods, no dependencies.
- DTOs SHOULD use `record` types for immutability and value equality when practical.
- Request DTOs MAY carry data annotation attributes for input validation.
