---
category: typescript
stack: typescript
status: Active
requires: []
conflicts_with: []
last_reviewed: 2026-07-29
---

# Named Exports Only

## Decision

All modules use named exports exclusively. Default exports are banned. Each component directory exposes a barrel `index.ts` that re-exports its public API.

## Rationale

- Named exports enforce a single canonical name for every symbol across the codebase. Default exports allow importers to rename freely, which leads to the same function being called three different things in three files.
- Named exports enable reliable automated refactoring — rename a symbol and every import updates. Default exports break this because the import name is arbitrary.
- Barrel files provide a clean public API per component while hiding internal implementation details.

## Constraints (non-negotiable for AI)

- NEVER use `export default` — all exports MUST be named.
- Each component directory (e.g., `src/core/config/`) MUST have an `index.ts` barrel file exporting its public API.
- Internal helper functions that are not part of the public API MUST NOT be re-exported from the barrel file.
- Import from the barrel path (e.g., `from "../config"`) for cross-component usage, not from internal files (e.g., `from "../config/config-loader"`).
