# REST API Response Envelope

**Category:** api
**Status:** Active
**Requires:** —
**Conflicts with:** —

## Decision
All successful (2xx) API responses use a standard `{ data, meta }` envelope structure. This provides a consistent contract for frontend consumers.

## Rationale
- A uniform envelope means the frontend always knows where to find the payload and metadata
- Allows adding metadata (pagination, warnings, deprecation notices) without breaking the response shape
- Alternatives considered: flat responses with headers for metadata (rejected — harder to consume), JSON:API spec (rejected — overly complex for this project's needs)

## Constraints (non-negotiable for AI)
- Single item responses: `{ "data": { ... } }`
- List responses: `{ "data": [ ... ], "meta": { "totalCount": N, "page": N, "pageSize": N } }`
- Error responses are NOT wrapped in this envelope — they use the stack's Problem Details ADR (`adrs/dotnet/rfc7807-errors.md` or `adrs/python/rfc7807-errors.md`) with `application/problem+json`
- Never return a raw array or unwrapped object at the top level of a successful response
- Create generic response wrapper classes: `ApiResponse<T>` for single items, `ApiListResponse<T>` for lists
- Controllers must always wrap return values in the envelope
