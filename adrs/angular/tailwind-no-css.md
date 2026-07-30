# Tailwind CSS Only — No Component Stylesheets

**Category:** angular
**Status:** Active
**Requires:** —
**Conflicts with:** `adrs/react/tailwind-shadcn.md`

## Decision
All styling uses Tailwind CSS utility classes applied directly in templates. Component-level CSS/SCSS files are not used.

## Rationale
- Tailwind utility classes co-locate styling with markup, making components self-contained and easier to reason about
- Eliminates stylesheet sprawl and specificity conflicts
- Alternatives considered: component-scoped SCSS, CSS-in-JS — both rejected for adding unnecessary abstraction layers

## Constraints (non-negotiable for AI)
- Set `styles: []` in every `@Component` decorator (empty array, no stylesheet)
- Do not generate `.css` or `.scss` files for components
- Apply all styling via Tailwind utility classes in the template HTML
- Custom design tokens (colors, spacing, fonts) are defined CSS-first via `@theme` in the global stylesheet (Tailwind v4+); a `tailwind.config.js` is legacy — only for projects still on Tailwind v3
- `@apply` is permitted only in global stylesheets (`styles.css`) for truly reusable base patterns (e.g., `.btn-primary`)
- Never use inline `style` attributes for anything Tailwind can handle
