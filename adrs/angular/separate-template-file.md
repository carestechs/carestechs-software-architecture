---
category: angular
stack: angular
status: Active
requires:
  - adrs/angular/standalone-components.md
conflicts_with: []
last_reviewed: 2026-07-29
---

# Separate HTML Template File

## Decision

All component templates must be defined in a separate `.html` file referenced via `templateUrl`, never inline in the `@Component` decorator.

## Rationale

- Separate template files enforce a clear boundary between component logic (TypeScript) and presentation (HTML), making both easier to read and review
- Large inline templates clutter the component class and make diffs harder to parse in code reviews
- IDEs and AI agents can navigate, lint, and format `.html` files independently
- Alternatives considered: inline `template` strings — rejected because they reduce readability as templates grow and lose dedicated HTML tooling support

## Constraints (non-negotiable for AI)

- Every `@Component` must use `templateUrl` pointing to a co-located `.html` file
- NEVER use the inline `template` property in the `@Component` decorator
- The template file must be co-located with the component file and follow the naming convention `<component-name>.component.html`
- Keep template logic minimal — use component methods or pipes for complex expressions rather than bloating the template
