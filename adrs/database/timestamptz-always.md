# Always Use timestamptz for Datetime Columns

**Category:** database
**Stack:** any
**Status:** Active
**Requires:** —
**Conflicts with:** —
**Last reviewed:** 2026-07-29

## Decision
All datetime columns use `timestamptz` (TIMESTAMP WITH TIME ZONE) in PostgreSQL and `DateTimeOffset` in C#. All values are stored in UTC.

## Rationale
- `timestamptz` stores an absolute point in time, eliminating timezone ambiguity
- `timestamp` (without timezone) silently drops timezone context, leading to subtle bugs when servers or clients are in different timezones
- Alternatives considered: `timestamp` with application-level UTC convention (rejected — too easy to violate), storing Unix epoch integers (rejected — poor developer ergonomics and query readability)

## Constraints (non-negotiable for AI)
- All datetime columns in migrations must use `timestamptz`, never `timestamp`
- C# properties must use `DateTimeOffset`, never `DateTime`
- All values stored must be in UTC (`DateTimeOffset.UtcNow`)
- Never call `DateTime.Now` — always use `DateTimeOffset.UtcNow`
- Timezone conversion to local display time is a frontend-only concern
- EF Core column type should be explicitly configured as `timestamptz` if not inferred
- Npgsql caveat: it maps `timestamptz` to UTC values and throws when writing a `DateTimeOffset` with a non-zero offset — always write UTC values (`DateTimeOffset.UtcNow` or `.ToUniversalTime()`)
- Python stacks: use timezone-aware `datetime` objects in UTC (`datetime.now(timezone.utc)`); NEVER store naive datetimes
