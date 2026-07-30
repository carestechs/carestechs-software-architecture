---
category: database
stack: any
status: Active
requires: []
conflicts_with: []
last_reviewed: 2026-07-29
---

# UUID Primary Keys

## Decision
All primary keys are UUIDs, generated server-side or by the database. Auto-increment integers are never used for primary keys.

## Rationale
- UUIDs enable distributed ID generation without coordination between services or database nodes
- Prevents enumeration attacks (sequential IDs expose record counts and allow scraping)
- Alternatives considered: auto-increment integers (rejected for security and distribution concerns), ULIDs (rejected — UUIDv7 provides the same time-ordering benefits with native `uuid`-type ecosystem support)
- Purely random UUIDv4 keys fragment B-tree indexes under high insert rates; time-ordered UUIDv7 keeps inserts append-mostly, so prefer v7 where the platform provides it

## Constraints (non-negotiable for AI)
- All PK columns use `uuid` type in PostgreSQL, `Guid` in C#, and `uuid.UUID` in Python
- Generate IDs server-side, preferring UUIDv7 where available: `Guid.CreateVersion7()` (.NET 9+), `uuid.uuid7()` (Python 3.14+, or the `uuid-utils` package), `uuidv7()` (PostgreSQL 18+). `Guid.NewGuid()` / `uuid.uuid4()` / `gen_random_uuid()` remain acceptable where v7 is unavailable
- Never define a primary key as `int`, `long`, or `serial`
- Foreign keys referencing PKs must also be `uuid`/`Guid`
- EF Core entity configurations must specify `.ValueGeneratedOnAdd()` for UUID PKs when using database defaults

## Examples

**Violation — auto-increment integer key:**
```sql
create table products (
  id serial primary key
);
```

**Compliant:**
```sql
create table products (
  id uuid primary key default gen_random_uuid() -- uuidv7() on PostgreSQL 18+
);
```
