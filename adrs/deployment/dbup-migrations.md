---
category: deployment
stack: dotnet
status: Active
requires: []
conflicts_with:
  - adrs/deployment/flyway-migrations.md
last_reviewed: 2026-07-31
verify_against:
  - DbUp NuGet package
---

# DbUp Embedded SQL Migrations

## Decision
Database schema is managed by hand-written `V{N}__{name}.sql` scripts embedded as resources in a shared `Common.Database` assembly and applied by DbUp. Migrations run in two ways: automatically when a tenant is provisioned (the provisioner replays the full history into the fresh database), and operator-driven via a console project (`Common.DatabaseCli`) against a chosen environment. EF Core remains a runtime-only ORM — no EF migrations.

## Rationale
- Embedding scripts in an assembly means the migration set ships inside the deployable — the provisioner Lambda and the operator CLI both carry the exact history they apply, with no external files or toolchain to distribute.
- DbUp is a .NET library, not a separate runtime: no Java/Flyway installation on operator machines or CI images, and the tenant provisioner can apply migrations in-process. In a database-per-tenant model this matters — migrations are applied N times per release, programmatically.
- Hand-written SQL keeps DDL reviewable and deliberate, exactly as in the Flyway variant; the difference is packaging and the programmatic application path.
- Alternatives considered: Flyway (the sibling ADR — right choice when a single shared database is migrated from CI and the Java toolchain is acceptable), EF Core migrations (rejected for this family — generated DDL and snapshot drift take control away from review).

## Constraints (non-negotiable for AI)
- NEVER add EF Core migrations to projects in this family. `Common.Database` scripts are the only schema source.
- Scripts are immutable once merged. Fixes are new `V{N+1}__` scripts — NEVER edit an applied script (the journal hash would diverge across tenant databases).
- Scripts MUST be marked as embedded resources; DbUp reads them from the assembly, not the filesystem.
- The DbUp journal table (`SchemaVersions`) lives in the `public` schema and is owned by DbUp — never write to it manually.
- Tenant provisioning MUST run the same DbUp pipeline as the operator CLI — one code path for both entry points.
- Migration scripts MUST be idempotence-agnostic: correctness comes from the journal, not from `IF NOT EXISTS` guards sprinkled through DDL.

## Examples

**Violation — schema change outside the migration pipeline:**
```csharp
// "quick fix" in a handler
await db.Database.ExecuteSqlRawAsync("ALTER TABLE users ADD COLUMN last_seen timestamptz");
```

**Compliant:**
```sql
-- Common.Database/Scripts/V42__users_add_last_seen.sql (embedded resource)
ALTER TABLE identity.users ADD COLUMN last_seen timestamptz NULL;
```
