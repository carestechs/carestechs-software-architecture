# Offset-Based Pagination

**Category:** api
**Status:** Active
**Requires:** `adrs/api/rest-envelope.md`
**Conflicts with:** —

## Decision
All list endpoints use offset-based pagination with `page` and `pageSize` query parameters. Sorting is supported via `sortBy` and `sortDir` parameters.

## Rationale
- Offset pagination is simple to implement, understand, and integrates naturally with SQL OFFSET/LIMIT
- Sufficient for datasets where real-time consistency during paging is not critical
- Alternatives considered: cursor-based pagination (rejected — adds complexity not justified by current scale), keyset pagination (rejected — requires careful index design for each endpoint)

## Constraints (non-negotiable for AI)
- `page` parameter is 1-based (first page is `page=1`)
- `pageSize` defaults to 20 if not provided
- `pageSize` maximum is 100 — reject requests exceeding this with a 400 error
- Response `meta` must include: `totalCount`, `page`, `pageSize`
- Sorting parameters: `sortBy` (column name), `sortDir` (`asc` or `desc`, default `asc`)
- Validate `sortBy` against an allowlist of sortable columns — never pass raw user input to ORDER BY
- Create a shared `PaginationParams` class for binding query parameters
- Use `.Skip((page - 1) * pageSize).Take(pageSize)` for EF Core queries
