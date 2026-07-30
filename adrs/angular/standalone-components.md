---
category: angular
stack: angular
status: Active
requires: []
conflicts_with:
  - adrs/react/functional-components.md
last_reviewed: 2026-07-29
---

# Standalone Components Only

## Decision
All components, directives, and pipes must be standalone. NgModules are not used anywhere in the application.

## Rationale
- Standalone components simplify the dependency graph, reduce boilerplate, and make lazy loading straightforward
- NgModules add indirection and make it harder for both humans and AI to trace what a component depends on
- Alternatives considered: traditional NgModule-based architecture, SCAM (Single Component Angular Module) pattern — both rejected for unnecessary complexity

## Constraints (non-negotiable for AI)
- Standalone is the default since Angular v19 — NEVER set `standalone: false`, and do not add a redundant `standalone: true` on v19+ (on older Angular versions, every `@Component`, `@Directive`, and `@Pipe` must set it explicitly)
- Dependencies are declared in the `imports` array directly on the component decorator
- Route configuration must use `loadComponent` for lazy loading individual components
- Never generate or reference an `NgModule` class
- Never use `loadChildren` pointing to a module — use `loadComponent` or route files with `default export`
