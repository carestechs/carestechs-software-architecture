# Standalone Components Only

**Category:** angular
**Status:** Active
**Requires:** —
**Conflicts with:** —

## Decision
All components, directives, and pipes must be standalone. NgModules are not used anywhere in the application.

## Rationale
- Standalone components simplify the dependency graph, reduce boilerplate, and make lazy loading straightforward
- NgModules add indirection and make it harder for both humans and AI to trace what a component depends on
- Alternatives considered: traditional NgModule-based architecture, SCAM (Single Component Angular Module) pattern — both rejected for unnecessary complexity

## Constraints (non-negotiable for AI)
- Every `@Component`, `@Directive`, and `@Pipe` must include `standalone: true`
- Dependencies are declared in the `imports` array directly on the component decorator
- Route configuration must use `loadComponent` for lazy loading individual components
- Never generate or reference an `NgModule` class
- Never use `loadChildren` pointing to a module — use `loadComponent` or route files with `default export`
