# TypeScript Strict Mode with No Escape Hatches

**Category:** typescript
**Status:** Active

## Decision

All TypeScript code uses strict mode with no escape hatches. The `any` type and suppression directives are banned. Prefer `unknown` with type guards when dealing with genuinely untyped data.

## Rationale

- Strict mode catches an entire class of runtime bugs at compile time — null dereferencing, implicit any, unchecked index access. The cost is slightly more verbose code; the benefit is dramatically fewer runtime surprises.
- Alternatives considered: partial strict (e.g., strict without `noUncheckedIndexedAccess`) — rejected because every hole in the type system becomes a place bugs hide. Full strict from day one is cheaper than migrating later.
- `any` is contagious — one `any` silently disables type checking for everything it touches downstream.

## Constraints (non-negotiable for AI)

- `tsconfig.json` MUST set `"strict": true` and `"noUncheckedIndexedAccess": true`.
- NEVER use the `any` type — use `unknown` with type guards or narrowing instead.
- NEVER use `@ts-ignore` or `@ts-expect-error` directives.
- NEVER use non-null assertion operator (`!`) unless the surrounding code makes the invariant obvious (e.g., immediately after a null check in a map).
- All function parameters and return types MUST be explicitly typed (no reliance on inference for public API).
