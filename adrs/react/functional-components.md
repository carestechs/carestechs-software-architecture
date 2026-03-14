# Functional Components with Hooks Only

**Category:** react
**Status:** Active
**Requires:** —
**Conflicts with:** `adrs/angular/standalone-components.md`

## Decision
All components are functional components using hooks for state, effects, and context. Class components are never used. Components are organized by feature in a `features/` directory, with shared reusable components in `shared/`.

## Rationale
- Functional components with hooks are the standard React pattern since React 16.8. They are simpler, more composable, and better supported by the React team and ecosystem.
- Alternatives considered: class components (rejected — more boilerplate, no hooks support, officially discouraged for new code), mixed class/functional (rejected — inconsistent codebase, harder for new developers).
- Hooks enable clean separation of concerns: `useState` for local state, `useEffect` for side effects, `useContext` for shared state, and custom hooks for reusable logic.
- Feature-based folder organization scales better than component-type folders (e.g., `components/`, `containers/`) as the application grows.

## Constraints (non-negotiable for AI)
- All components MUST be functional components (`function ComponentName()` or `const ComponentName = () =>`). NEVER use class components.
- State MUST be managed with `useState` or `useReducer`. NEVER use `this.state` or `this.setState`.
- Side effects MUST use `useEffect`. NEVER use lifecycle methods (`componentDidMount`, etc.).
- Feature components MUST live in `src/features/<feature>/` directories.
- Shared reusable components MUST live in `src/components/` (or `src/shared/`).
- Components MUST be exported as named exports. Default exports are reserved for route-level page components only.
- Custom hooks MUST be prefixed with `use` and live in a `hooks/` directory within their feature or in a shared `src/hooks/` directory.
