---
category: python
stack: python
status: Active
requires: []
conflicts_with:
  - adrs/dotnet/structured-logging.md
last_reviewed: 2026-07-30
---

# Structured Logging via stdlib logging

## Decision
All logging goes through the standard library `logging` module with per-module loggers (`logging.getLogger(__name__)`). Structured fields ride on the `extra` mapping; production handlers emit JSON via a single formatter configured once in `core/` (`logging.config.dictConfig`). Every request carries a correlation ID injected by middleware and included on every record for that request.

## Rationale
- stdlib `logging` is the ecosystem's common denominator: every library already logs through it, uvicorn/celery integrate with it, and a JSON formatter turns it into aggregator-ready structured output without adopting a second logging framework.
- Alternatives considered: structlog (viable — richer structured pipeline; adds a dependency and a second idiom on top of stdlib logging, which every third-party library still uses), loguru (rejected — replaces rather than integrates with the stdlib ecosystem), `print()` (rejected — no levels, no structure, invisible to handlers).
- Lazy `%s` formatting (or `extra` fields) defers string work until a record is actually emitted and keeps the message template stable for aggregation; f-strings evaluate eagerly and make every message unique.
- One `dictConfig` in `core/` keeps configuration coherent; per-module `basicConfig` calls fight each other and duplicate handlers.

## Constraints (non-negotiable for AI)
- Every module MUST obtain its logger via `logging.getLogger(__name__)` — NEVER use `print()` for diagnostics and never log through the root logger directly.
- Log calls MUST use lazy formatting (`logger.info("order %s created", order_id)`) and/or structured fields via `extra={...}` — NEVER f-strings or `.format()` inside log calls.
- Exceptions MUST be logged with `logger.exception(...)` (or `exc_info=True`) — NEVER embedded into the message text.
- Production handlers MUST emit one JSON object per record via a single formatter; human-readable format is development-only.
- Logging configuration (`dictConfig`) lives once in `core/` — modules MUST NOT call `basicConfig` or add handlers.
- API middleware MUST attach a correlation/request ID to every record emitted while handling a request (contextvar-based filter); background tasks propagate the ID from the triggering message.
- NEVER log secrets, tokens, connection strings, passwords, or PII. Log entity IDs, not entity payloads.
- Log levels: `INFO` for business events, `WARNING` for handled anomalies, `ERROR` for failures — NEVER `ERROR` for expected business outcomes (e.g., validation failures).

## Examples

**Violation — print and eager f-string formatting:**
```python
print(f"processing order {order.id}")
logger.error(f"failed to ship order {order.id}: {exc}")
```

**Compliant:**
```python
logger = logging.getLogger(__name__)
logger.info("processing order %s", order.id)
logger.exception("failed to ship order %s", order.id)  # inside an except block
```
