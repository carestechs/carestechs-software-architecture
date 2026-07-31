---
category: database
stack: any
status: Active
requires: []
conflicts_with: []
last_reviewed: 2026-07-31
---

# PostgreSQL Schema Per Module

## Decision
Within each database, every module owns a PostgreSQL schema named after it (`identity.users`, `messaging.communication`). The module's ORM context declares that schema as its default; migration scripts create objects inside it. The migration journal stays in `public`. Optionally — and recommended for full enforcement — each module gets a database role with grants only on its own schema.

## Rationale
- A shared flat schema makes module boundaries a matter of code review; schemas make them visible in the database and, with role grants, mechanically enforced: a module reaching into another module's tables fails with `insufficient_privilege` at the protocol level rather than passing review unnoticed.
- Module-to-schema mapping keeps extraction honest: a module promoted to its own service takes its schema with it — the table inventory is already isolated.
- Alternatives considered: flat shared schema with naming discipline (rejected — audits repeatedly find drift; discipline does not survive team growth), database-per-module (rejected inside a tenant database model — multiplies databases by modules and breaks single-transaction module writes).
- One line of ORM configuration (`HasDefaultSchema("foo")` in EF Core, `MetaData(schema="foo")` in SQLAlchemy) qualifies every mapped table; the change is cheap for new modules and a mechanical `ALTER TABLE ... SET SCHEMA` migration for existing ones.

## Constraints (non-negotiable for AI)
- Every table belongs to exactly one module's schema. NEVER create module tables in `public` — `public` holds only the migration journal and extensions.
- A module's ORM context MUST declare its own schema as default and MUST NOT map tables from another module's schema.
- NEVER write cross-schema queries or joins outside the owning module. Cross-module data access goes through the owning module's contract interface.
- Foreign keys MUST NOT cross module schemas — cross-module references are ID-only (see the cross-module-by-id rule).
- When the per-module role split is in place, application components MUST connect with their module's role, not a superuser.
- Schema moves are migrations like any other: `CREATE SCHEMA IF NOT EXISTS foo; ALTER TABLE bar SET SCHEMA foo;` — never manual DDL.
