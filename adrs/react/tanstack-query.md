# TanStack Query for Server State

**Category:** react
**Stack:** react
**Status:** Active
**Requires:** `adrs/react/functional-components.md`
**Conflicts with:** `adrs/angular/signals-state.md`
**Last reviewed:** 2026-07-29

## Decision
All server state (data fetched from APIs) is managed by TanStack Query (React Query). Local UI state uses `useState`/`useReducer`. TanStack Query handles caching, background refetching, stale-while-revalidate, optimistic updates, and loading/error states.

## Rationale
- TanStack Query is the industry standard for server state management in React. It eliminates manual loading/error state tracking, provides automatic caching and background refetching, and handles cache invalidation declaratively.
- Alternatives considered: `useEffect` + `useState` (rejected — leads to boilerplate, race conditions, and inconsistent loading/error patterns), Redux Toolkit Query (rejected — heavier, Redux dependency unnecessary for this use case), SWR (rejected — smaller feature set: no comparable mutation/cache-invalidation model).
- Separating server state (TanStack Query) from client state (`useState`) provides a clean mental model. Server state is cached, refetchable, and shared across components. Client state is local and ephemeral.

## Constraints (non-negotiable for AI)
- All API data fetching MUST use `useQuery` or `useSuspenseQuery`. NEVER fetch data with raw `useEffect` + `fetch`.
- All API mutations (POST, PUT, DELETE) MUST use `useMutation` with appropriate `onSuccess` cache invalidation.
- Query keys MUST be structured arrays (e.g., `['explorations', id]`). NEVER use plain strings as query keys.
- Query functions MUST be defined in dedicated API service files (e.g., `src/features/explorations/api.ts`), not inline in components.
- `QueryClient` MUST be configured once in the app root and provided via `QueryClientProvider`.
- NEVER store server-fetched data in `useState` or global state — let TanStack Query own it.
- Loading and error states MUST be handled using the v5 hook properties — `isPending` for the initial load (mutations also expose `isPending`; `isLoading` no longer exists on v5 mutations), `isError`, and `error` — or via React Suspense boundaries with `useSuspenseQuery`.
