# Snake Case Database Naming Convention

**Category:** database
**Status:** Active
**Requires:** —
**Conflicts with:** —

## Decision
All database tables and columns use snake_case naming. C# properties remain PascalCase — the EF Core naming convention package handles translation automatically.

## Rationale
- snake_case is the PostgreSQL idiomatic convention and avoids quoting issues with mixed-case identifiers
- Automatic translation via naming convention package eliminates manual column mapping
- Alternatives considered: PascalCase in database (rejected — requires quoting in raw SQL), manual `[Column("name")]` attributes (rejected — tedious and error-prone)

## Constraints (non-negotiable for AI)
- Configure `UseSnakeCaseNamingConvention()` on the DbContext (via Npgsql EF Core package)
- C# entity properties use PascalCase as normal (e.g., `CreatedAt`, `UserId`)
- Never manually specify `[Column("...")]` or `[Table("...")]` attributes for snake_case translation — the convention handles it
- Raw SQL queries must use snake_case identifiers (e.g., `created_at`, `user_id`)
- Migration files will reflect snake_case names — this is expected and correct
