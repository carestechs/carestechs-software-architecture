---
category: dotnet
stack: dotnet
status: Active
requires:
  - adrs/dotnet/cqrs-handlers.md
conflicts_with:
  - adrs/dotnet/rfc7807-errors.md
last_reviewed: 2026-07-29
---

# Result Pattern for Error Handling

## Decision

Command handlers return `Result<T>` or `Result` instead of throwing exceptions for expected failures. The `Result` type carries `IsSuccess`, `Error` (with `ErrorType` enum: NotFound, Validation, Conflict, etc.), and an optional `ErrorMessage`. Endpoints inspect the result and map to appropriate HTTP responses. Exceptions are reserved for truly unexpected failures.

## Rationale

- The Result pattern makes failure an explicit part of the return type. Callers cannot accidentally ignore an error because the compiler forces them to handle the `Result`. This eliminates the hidden control flow of exceptions for expected business failures (not found, validation errors, conflicts).
- Alternatives considered: throwing exceptions for all errors with a global exception handler (rejected — exceptions are expensive, create hidden control flow, and conflate expected failures with unexpected bugs), FluentResults library (acceptable but adds a dependency — a simple hand-rolled `Result<T>` covers our needs), nullable returns (rejected — null doesn't carry error context).
- The `Error` record type includes an `ErrorType` enum that maps cleanly to HTTP status codes at the endpoint level: `NotFound → 404`, `Validation → 400`, `Conflict → 409`, `Unauthorized → 401`.
- Factory methods (`Result<T>.Success(value)`, `Result<T>.Failure(error, message)`) make the intent explicit at the call site.
- The conflict with `rfc7807-errors.md` concerns the in-process propagation mechanism (returned `Result` values vs thrown exceptions caught by a global handler) — not the wire format: endpoints mapping failures via `Results.Problem()` still emit `application/problem+json`.

## Constraints (non-negotiable for AI)

- Command handlers MUST return `Result<T>` (for create operations returning an ID) or `Result` (for update/delete operations).
- Query handlers return the DTO directly — nullable for single-item lookups (e.g., `Task<EntityContext?>`), with endpoints mapping `null` to 404. Do NOT wrap query reads in `Result`.
- NEVER throw exceptions for expected business failures (not found, validation errors, duplicate keys, authorization failures). Use `Result.Failure(...)` instead.
- The `Error` type MUST include an `ErrorType` enum with at least: `NotFound`, `Validation`, `Conflict`, `Unauthorized`, `Internal`, `None`.
- Endpoints MUST inspect `result.IsSuccess` and map failures to appropriate HTTP responses (e.g., `Results.Problem()` or `Results.NotFound()`).
- Exceptions are ONLY for truly unexpected failures (network errors, null reference bugs, configuration errors). These propagate to global error handling.
- NEVER catch and swallow exceptions inside handlers — let unexpected failures bubble up.
- Generic error factory methods (`GenericErrors.NotFound(id, description)`, `GenericErrors.Validation(id, description)`) MUST be used for consistent error creation; `GenericErrors` lives in `Common.Lib/Errors/` alongside the `Error` record.
