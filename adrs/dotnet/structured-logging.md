---
category: dotnet
stack: dotnet
status: Active
requires: []
conflicts_with:
  - adrs/python/structured-logging.md
last_reviewed: 2026-07-30
---

# Structured Logging via ILogger

## Decision
All logging goes through the injected `ILogger<T>` abstraction using structured message templates with named placeholders. Production output is structured JSON (console formatter) so container platforms and log aggregators can parse and query it. Every request carries a correlation ID included in the log scope. Provider configuration lives in the host's composition root; modules depend only on `ILogger<T>`.

## Rationale
- `ILogger<T>` is the built-in .NET logging abstraction: providers (JSON console, Serilog, OpenTelemetry) are swappable in the composition root without touching module code — the same pattern the stack already uses for LLM clients and configuration.
- Message templates (`"Order {OrderId} shipped"`) preserve structure: aggregators can group by template and query by field. String interpolation collapses every occurrence into a unique string, destroying both.
- Alternatives considered: Serilog as a direct dependency in modules (rejected — provider choice belongs in the host; Serilog can still back `ILogger<T>` from the composition root), `Console.WriteLine` (rejected — no levels, no structure, no scopes).
- Correlation IDs turn a distributed trace of one request into a single queryable thread — essential once workers and queues process work asynchronously.

## Constraints (non-negotiable for AI)
- All logging MUST use an injected `ILogger<T>` — NEVER `Console.WriteLine`, `Debug.WriteLine`, or static logger instances.
- Log calls MUST use message templates with named placeholders — NEVER string interpolation or concatenation inside the message (`$"..."` in a log call is a violation).
- Exceptions MUST be passed as the exception argument (`_logger.LogError(ex, "...")`) — NEVER embedded into the message text.
- Production logging output MUST be structured JSON (e.g., the built-in JSON console formatter); human-readable console format is development-only.
- Every HTTP request MUST carry a correlation ID (W3C `traceparent` or `HttpContext.TraceIdentifier`) attached via log scope so all records of one request are linkable; background jobs propagate the ID from the triggering message.
- NEVER log secrets, tokens, connection strings, passwords, or PII. Log entity IDs, not entity payloads.
- Log levels: `Information` for business events, `Warning` for handled anomalies, `Error` for failures — NEVER `Error` for expected business outcomes (e.g., validation failures).
- Logging providers and levels are configured only in the host (`Program.cs` / `appsettings.json`) — modules MUST NOT configure providers.

## Examples

**Violation — interpolated message, exception in the text:**
```csharp
Console.WriteLine($"error processing order {order.Id}: {ex.Message}");
_logger.LogError($"Failed to ship order {order.Id}");
```

**Compliant:**
```csharp
_logger.LogError(ex, "Failed to ship order {OrderId}", order.Id);
```
