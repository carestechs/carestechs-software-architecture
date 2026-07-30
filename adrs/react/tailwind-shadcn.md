# Tailwind CSS with shadcn/ui Components

**Category:** react
**Status:** Active
**Requires:** `adrs/react/functional-components.md`
**Conflicts with:** `adrs/angular/tailwind-no-css.md`

## Decision
All styling uses Tailwind CSS utility classes. Pre-built UI components come from shadcn/ui, which provides copy-paste Radix-based primitives styled with Tailwind. No CSS modules, styled-components, or other CSS-in-JS solutions are used. Component-level CSS files are not created.

## Rationale
- Tailwind CSS is the industry standard utility-first CSS framework. Combined with shadcn/ui, it provides a complete design system with accessible, customizable components that the team fully owns (no runtime dependency on a component library).
- Alternatives considered: Material UI (rejected — opinionated design system, heavy bundle, hard to customize deeply), Chakra UI (rejected — runtime CSS-in-JS, less performant), CSS Modules (rejected — scattered styles, harder to maintain consistency), styled-components (rejected — runtime overhead, separate styling layer).
- shadcn/ui components are copied into the project, not installed as a dependency. This means full ownership, no version lock-in, and complete control over customization.
- Tailwind's utility classes eliminate context switching between component logic and stylesheet files.

## Constraints (non-negotiable for AI)
- All styling MUST use Tailwind CSS utility classes in JSX `className` attributes.
- NEVER create component-level CSS files (`.css`, `.scss`, `.module.css`).
- NEVER use CSS-in-JS libraries (styled-components, emotion, etc.).
- UI primitives (buttons, inputs, dialogs, dropdowns, etc.) MUST come from shadcn/ui. NEVER build custom versions of components that shadcn/ui already provides.
- shadcn/ui components MUST live in `src/components/ui/` following the standard shadcn convention.
- Use the `cn()` utility (from `lib/utils.ts`) to merge Tailwind classes conditionally. NEVER use string concatenation for conditional classes.
- Design tokens (colors, spacing, typography) MUST be defined as CSS variables under `@theme` in the global stylesheet (Tailwind v4 — current shadcn/ui setups have no Tailwind config file; `tailwind.config.ts` applies only to legacy Tailwind v3 projects). NEVER use arbitrary values when a design token exists.
