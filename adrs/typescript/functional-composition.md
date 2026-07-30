# Functional Composition over Classes

**Category:** typescript
**Stack:** typescript
**Status:** Active
**Requires:** —
**Conflicts with:** —
**Last reviewed:** 2026-07-29

## Decision

Prefer plain functions and plain objects over classes. Use function composition for building behavior. Classes are only permitted when mutable state and behavior genuinely belong together (e.g., custom Error subclasses).

## Rationale

- Functions are simpler to test — no instantiation, no lifecycle, no `this` binding issues. A function that takes input and returns output is the easiest unit to reason about.
- TypeScript's structural type system works naturally with plain objects and interfaces. Classes add nominal typing concerns, inheritance complexity, and decorator magic that obscure control flow.
- Alternatives considered: OOP with dependency injection containers (rejected — adds framework weight and indirection for tools that are fundamentally pipelines of data transformation), hybrid approach with classes for services (rejected — service classes with a single public method are just functions with extra ceremony).

## Constraints (non-negotiable for AI)

- NEVER create a class unless it holds mutable state that genuinely requires encapsulation (e.g., custom Error subclass, stateful builder).
- NEVER use class inheritance chains — prefer function composition or interface implementation.
- NEVER use decorators for control flow or dependency injection.
- Service logic MUST be implemented as exported functions, not as class methods.
- Configuration and data MUST be plain objects conforming to interfaces/types, not class instances.
