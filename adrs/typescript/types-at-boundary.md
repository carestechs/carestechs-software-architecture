# Interfaces for Contracts, Types for Data

**Category:** typescript
**Status:** Active
**Requires:** `adrs/typescript/strict-typescript.md`

## Decision

Use `interface` to define component contracts (what a component exposes to consumers). Use `type` to define data shapes (what flows between components). All data types must be JSON-serializable plain objects.

## Rationale

- The distinction between contracts and data clarifies intent: an `interface` says "you can depend on this shape", a `type` says "this is what the data looks like". This helps AI and developers reason about which types are stable API surfaces vs. internal data representations.
- Interfaces support declaration merging and `extends`, making them natural for component APIs that may grow. Type aliases support unions, intersections, and mapped types, making them natural for data transformations.
- JSON-serializable data types ensure that outputs can be rendered to terminal, PR comments, or files without serialization surprises (no Date objects, no class instances, no functions).

## Constraints (non-negotiable for AI)

- Component public APIs (what a module exposes to other modules) MUST use `interface`.
- Data shapes flowing between components (config, findings, diffs, etc.) MUST use `type`.
- All data types MUST be JSON-serializable — no class instances, no `Date` objects (use ISO strings), no functions, no circular references.
- Use `Readonly<>` for types that represent immutable data (config, resolved docs).
- NEVER mix concerns: a single type should not serve as both a component contract and a data transfer shape.
