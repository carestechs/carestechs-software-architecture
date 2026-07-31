---
category: deployment
stack: dotnet
status: Active
requires: []
conflicts_with:
  - adrs/deployment/dbup-migrations.md
last_reviewed: 2026-07-29
---

# Flyway SQL Migrations

## Decision

Database schema is managed via Flyway with hand-written SQL migration scripts. Migrations follow the naming convention `V<number>__<Description>.sql` and live in a shared `Common.Database/db/` directory. EF Core is used only as an ORM for runtime queries — it does not generate or apply migrations. All table and column names in migration scripts are lowercase (PostgreSQL convention for unquoted identifiers).

## Rationale

- Hand-written SQL migrations give full control over the schema, including indexes, constraints, partial indexes, and database-specific features that EF Core's migration generator cannot express or may generate suboptimally.
- Alternatives considered: EF Core migrations (rejected — generated SQL is often suboptimal, merge conflicts in migration snapshots are painful in teams, and the migration model file grows unbounded), raw SQL scripts without a runner (rejected — no versioning, no idempotency checks, no rollback tracking).
- Flyway tracks applied migrations in a `flyway_schema_history` table, ensuring each migration runs exactly once and in order. This is safer than manual script execution.
- Sharing a single migration directory (`Common.Database/db/`) across modules ensures all tables are created in the same database with a unified migration history, even when accessed by separate DbContexts.

## Constraints (non-negotiable for AI)

- All schema changes MUST be expressed as Flyway migration scripts in `Common.Database/db/`.
- Migration files MUST follow the naming convention `V<number>__<Description>.sql` (e.g., `V8__Add_SiteManagement_Tables.sql`).
- NEVER use EF Core migrations (`dotnet ef migrations add`) — EF Core is runtime-only.
- All table and column names in SQL scripts MUST be lowercase, unquoted. PostgreSQL folds unquoted identifiers to lowercase.
- The DbContext MUST apply a lowercase naming loop in `OnModelCreating` to match Flyway-created schema: table names, column names, key names, FK constraint names, and index names.
- Each module's DbContext MUST only map its own tables — it coexists with other modules' tables in the same database but is unaware of them.
- Migration scripts MUST rely on Flyway's schema history for run-once semantics: use plain `CREATE TABLE` (NOT `CREATE TABLE IF NOT EXISTS`) so that schema drift fails loudly instead of being silently masked.
