---
category: database
stack: any
status: Active
requires: []
conflicts_with: []
last_reviewed: 2026-07-31
---

# DynamoDB for Hot-Path Document Data

## Decision
High-throughput, conversation-shaped data (messages, session state, caches) lives in DynamoDB with tenant-scoped partition keys. PostgreSQL remains the source of truth for relational entities and their lifecycle. Each table has exactly one owning module; access goes through that module's repositories.

## Rationale
- Per-conversation message storage is append-heavy, read-by-partition, and unbounded — the access pattern DynamoDB's partition model fits exactly, and the pattern that turns relational tables into vacuum and index-bloat problems.
- Alternatives considered: everything in PostgreSQL (rejected — hot tables grow without bound and the relational features go unused), everything in DynamoDB (rejected — tenancy, users, configuration are relational and transactional; modeling them as items produces join-emulation in application code), a cache layer over PostgreSQL (rejected — caches with TTLs introduce staleness windows in message delivery paths).
- Key design is the schema: get it wrong and there is no `ALTER TABLE`. Partition keys embed tenant scope; sort keys order the aggregate's items. Global secondary indexes are deliberate, named design decisions — not an escape hatch added per query.
- Document attributes are schemaless at the store but NOT in the application: the item shape is a versioned contract. Ad-hoc shape changes strand old items.

## Constraints (non-negotiable for AI)
- NEVER `Scan` on a request or worker hot path. Every runtime read is a `Query` by partition key (optionally sort-key-bounded) or a `GetItem`.
- Partition keys MUST embed tenant identifiers (e.g., `{org}#{workspace}#{conversationId}`). A key that can address another tenant's items is a defect.
- Each table has ONE owning module. Other modules obtain the data through the owning module's contract, never through their own DynamoDB client against a foreign table.
- Normalize empty strings to absent attributes (or explicit null) at the repository boundary and handle both on read — DynamoDB's null/empty/missing distinctions MUST NOT leak into domain code.
- Document the item shape (attributes, types, GSI projections) next to the repository, and version it when it changes. Readers MUST tolerate the previous shape during rollout.
- Table names are plain constants in the owning repository. No environment-suffix indirection in runtime code — environment separation lives at the account/stack level.
