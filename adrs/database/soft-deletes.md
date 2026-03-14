# Soft Deletes via deleted_at Column

**Category:** database
**Status:** Active
**Requires:** —
**Conflicts with:** —

## Decision
Entities support soft deletion through a nullable `deleted_at` (timestamptz) column. Application code never performs hard deletes.

## Rationale
- Soft deletes preserve audit trails and allow recovery of accidentally deleted data
- Global query filters ensure soft-deleted records are invisible by default without per-query boilerplate
- Alternatives considered: separate archive tables (rejected — complicates schema), boolean `is_deleted` flag (rejected — timestamp provides more information)

## Constraints (non-negotiable for AI)
- Add a nullable `DateTimeOffset? DeletedAt` property to all soft-deletable entities
- Maps to `deleted_at` column of type `timestamptz` in PostgreSQL
- Configure EF Core global query filters: `.HasQueryFilter(e => e.DeletedAt == null)`
- To soft-delete: set `DeletedAt = DateTimeOffset.UtcNow`, never call `Remove()` or `DELETE`
- Hard deletes (`DELETE FROM`) are only permitted in background data compaction/cleanup jobs, never from application code
- To query including soft-deleted records, use `.IgnoreQueryFilters()` explicitly
