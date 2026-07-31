---
category: database
stack: any
status: Active
requires: []
conflicts_with: []
last_reviewed: 2026-07-31
---

# Database-Per-Tenant Isolation

## Decision
Each tenant (organization + workspace) gets its own physical PostgreSQL database; document-store data (if present) uses tenant-scoped partition keys. Application code never holds a global connection: it opens a per-tenant unit of work through a factory that builds the connection string from validated tenant identifiers at request/message time.

## Rationale
- Database-per-tenant makes cross-tenant data access impossible at the connection level — there is no WHERE clause to forget. For multi-tenant platforms handling third parties' customer data, this is the strongest practical isolation short of separate infrastructure.
- Alternatives considered: shared database with a `tenant_id` column on every table (rejected — one missed filter is a data breach, and per-tenant backup/restore/deletion becomes surgery), schema-per-tenant in one database (rejected — connection pools and migrations scale poorly past dozens of tenants, and a dropped schema is easier to fat-finger than a dropped database), row-level security (viable hardening on top, but RLS policies are still per-table opt-in).
- Per-tenant databases make tenant lifecycle operations first-class: provisioning applies the full migration history to a fresh database; offboarding is a database drop plus object-storage prefix removal.
- The cost is real: N databases means N migration runs and connection-pool pressure. The per-tenant unit-of-work factory with an explicit pool policy per workload (API vs worker) keeps this manageable.

## Constraints (non-negotiable for AI)
- Tenant identifiers MUST come from validated sources — token claims for API requests, message metadata stamped by a trusted producer for queue work. NEVER from client-controlled fields (body, query string, headers).
- Connections MUST be opened through the per-tenant factory (`OpenForTenant(orgId, workspaceId)` or equivalent). NEVER a statically configured connection string to a tenant database.
- NEVER write a query that spans tenants. Cross-tenant aggregation is an offline/analytics concern fed by events, not a runtime query.
- Tenant provisioning MUST apply the complete migration history to the new database — a new tenant's schema is identical to the oldest tenant's.
- Document-store partition keys MUST embed the tenant identifiers (e.g., `{org}#{workspace}#...`) so tenant scoping is part of the key, not a filter.
- Pass tenant identifiers as explicit parameters through the call chain. NEVER resolve them from ambient/static state below the entry point.
- The tenant directory itself (organizations, workspaces, provisioning state) is the ONE deliberately global dataset: it lives in a global store owned by the tenancy module and is the only data addressable before tenant scope is established. Nothing else may be global by convenience.
