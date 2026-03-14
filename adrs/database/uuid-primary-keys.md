# UUID Primary Keys

**Category:** database
**Status:** Active
**Requires:** —
**Conflicts with:** —

## Decision
All primary keys are UUIDs, generated server-side or by the database. Auto-increment integers are never used for primary keys.

## Rationale
- UUIDs enable distributed ID generation without coordination between services or database nodes
- Prevents enumeration attacks (sequential IDs expose record counts and allow scraping)
- Alternatives considered: auto-increment integers (rejected for security and distribution concerns), ULIDs (rejected for insufficient ecosystem support in .NET/PostgreSQL)

## Constraints (non-negotiable for AI)
- All PK columns use `uuid` type in PostgreSQL and `Guid` in C#
- Generate IDs server-side with `Guid.NewGuid()` or use database default `gen_random_uuid()`
- Never define a primary key as `int`, `long`, or `serial`
- Foreign keys referencing PKs must also be `uuid`/`Guid`
- EF Core entity configurations must specify `.ValueGeneratedOnAdd()` for UUID PKs when using database defaults
