# Problem Details (RFC 9457) for API Errors

**Category:** dotnet
**Status:** Active
**Requires:** —
**Conflicts with:** `adrs/dotnet/result-pattern-errors.md`, `adrs/python/rfc7807-errors.md`

## Decision
All API errors use the Problem Details format (RFC 9457, which obsoletes RFC 7807). A global exception-handling middleware catches unhandled exceptions and returns structured `ProblemDetails` responses. Validation errors return HTTP 400 with field-level error details. The application uses ASP.NET Core's built-in `ProblemDetails` support — no custom error envelopes.

## Rationale
- Problem Details is an IETF standard (RFC 9457, obsoleting RFC 7807) for machine-readable error responses. Using it ensures consistency across all endpoints and gives API consumers a single, predictable error format to handle.
- Alternatives considered: custom error envelope (e.g., `{ "success": false, "errors": [...] }`) — rejected because it reinvents what Problem Details already standardizes and is not understood by off-the-shelf clients, gateways, and observability tooling that already speak `application/problem+json`. Plain status codes with no body (rejected — insufficient detail for client error handling).
- ASP.NET Core has first-class support for ProblemDetails via `builder.Services.AddProblemDetails()` and the `IExceptionHandler` interface, making adoption straightforward with no custom plumbing.
- Validation errors leverage ASP.NET Core's built-in model validation with automatic ProblemDetails formatting when `AddProblemDetails()` is configured.

## Constraints (non-negotiable for AI)
- All error responses produced by application code MUST use the `application/problem+json` content type and the `ProblemDetails` structure (body-less framework challenges, e.g., bare 401/403 from authentication middleware, are acceptable).
- Register `builder.Services.AddProblemDetails()` in `Program.cs`.
- Implement a global `IExceptionHandler` to catch unhandled exceptions, log them, and return a `ProblemDetails` response with a 500 status code (without leaking stack traces or internal details in production).
- Validation errors (400) MUST include field-level details in the `errors` extension property, using ASP.NET Core's built-in `ValidationProblemDetails`.
- NEVER create custom error response classes or envelopes.
- NEVER return raw exception messages or stack traces in production error responses.
- Use standard HTTP status codes: 400 for validation errors, 401 for authentication, 403 for authorization, 404 for not found, 409 for conflicts, 500 for unhandled server errors.
- Service layer methods MUST throw specific exceptions (e.g., `NotFoundException`, `ConflictException`) that the exception handler maps to appropriate HTTP status codes.
