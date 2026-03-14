# REST API Response Envelope

**Category:** api
**Status:** Active
**Requires:** —
**Conflicts with:** —

## Decision
All API responses use a standard `{ data, meta }` envelope structure. This provides a consistent contract for frontend consumers.

## Rationale
- A uniform envelope means the frontend always knows where to find the payload and metadata
- Allows adding metadata (pagination, warnings, deprecation notices) without breaking the response shape
- Alternatives considered: flat responses with headers for metadata (rejected — harder to consume), JSON:API spec (rejected — overly complex for this project's needs)

## Constraints (non-negotiable for AI)
- Single item responses: `{ "data": { ... } }`
- List responses: `{ "data": [ ... ], "meta": { "totalCount": N, "page": N, "pageSize": N } }`
- Error responses follow a separate error envelope (not covered by this ADR)
- Never return a raw array or raw object at the top level
- Create generic response wrapper classes: `ApiResponse<T>` for single items, `ApiListResponse<T>` for lists
- Controllers must always wrap return values in the envelope
