---
category: python
stack: python
status: Active
requires:
  - adrs/python/fastapi-framework.md
conflicts_with:
  - adrs/dotnet/rfc7807-errors.md
last_reviewed: 2026-07-29
---

# Problem Details (RFC 9457) for API Errors (Python)

## Decision
All API errors use the Problem Details format (RFC 9457, which obsoletes RFC 7807) with the `application/problem+json` content type. Global FastAPI exception handlers convert typed application exceptions into Problem Details responses. FastAPI's default error shape (`{"detail": ...}`) is overridden so that validation errors are also returned as Problem Details with field-level details.

## Rationale
- Problem Details is an IETF standard (RFC 9457, obsoleting RFC 7807) for machine-readable error responses. Using it gives API consumers a single, predictable error format — and keeps the error contract identical to the .NET stack's `rfc7807-errors` decision, so frontends work against both backends unchanged.
- FastAPI's out-of-the-box error shape (`{"detail": "..."}` and `{"detail": [...]}` for validation) is framework-specific and not Problem Details; leaving it in place produces two inconsistent error formats in one API.
- Alternatives considered: custom error envelope (e.g., `{ "success": false, "errors": [...] }`) — rejected because it reinvents what Problem Details already standardizes; FastAPI defaults as-is (rejected — inconsistent shape between validation errors and application errors, and not standard).
- Centralizing error mapping in exception handlers keeps route handlers and services free of HTTP formatting concerns: services raise typed exceptions, handlers translate them.

## Constraints (non-negotiable for AI)
- ALL error responses MUST use the `application/problem+json` content type and the Problem Details structure (`type`, `title`, `status`, `detail`, `instance`, plus extension members).
- Register global exception handlers (via `app.exception_handler(...)`, typically in `core/exceptions.py`) that map typed application exceptions (e.g., `NotFoundError`, `ConflictError`) to Problem Details responses with the correct status code.
- Override FastAPI's default handlers for `RequestValidationError` and `HTTPException` so validation errors return Problem Details with field-level details in an `errors` extension member — NEVER expose the default `{"detail": ...}` shape.
- Service functions MUST raise typed exceptions; route handlers MUST NOT catch exceptions to hand-build error responses.
- NEVER create custom error response classes or envelopes.
- NEVER return raw exception messages or stack traces in production error responses.
- Use standard HTTP status codes: 400/422 for validation errors, 401 for authentication, 403 for authorization, 404 for not found, 409 for conflicts, 500 for unhandled server errors.

## Examples

**Violation — FastAPI's default error shape:**
```json
{ "detail": "Order not found" }
```

**Compliant:**
```json
{
  "title": "Not Found",
  "status": 404,
  "detail": "Order 0198c9a1-... was not found."
}
// Content-Type: application/problem+json — emitted by the global exception handler
```
