---
category: database
stack: any
status: Active
requires: []
conflicts_with:
  - adrs/database/lowercase-naming.md
last_reviewed: 2026-07-29
---

# Snake Case Database Naming Convention

## Decision
All database tables and columns use snake_case naming. C# properties remain PascalCase — the EF Core naming convention package handles translation automatically.

## Rationale
- snake_case is the PostgreSQL idiomatic convention and avoids quoting issues with mixed-case identifiers
- Automatic translation via naming convention package eliminates manual column mapping
- Alternatives considered: PascalCase in database (rejected — requires quoting in raw SQL), manual `[Column("name")]` attributes (rejected — tedious and error-prone)
- When to choose: this ADR fits stacks where the ORM owns the schema (EF Core or Alembic migrations); choose `lowercase-naming` instead when the schema is hand-written SQL managed by Flyway

## Constraints (non-negotiable for AI)
- Configure `UseSnakeCaseNamingConvention()` on the DbContext (via Npgsql EF Core package)
- C# entity properties use PascalCase as normal (e.g., `CreatedAt`, `UserId`)
- Never manually specify `[Column("...")]` or `[Table("...")]` attributes for snake_case translation — the convention handles it
- Raw SQL queries must use snake_case identifiers (e.g., `created_at`, `user_id`)
- Migration files will reflect snake_case names — this is expected and correct
- Python/SQLAlchemy stacks: snake_case is the native convention — name table and column attributes snake_case directly in models (no translation layer needed)
