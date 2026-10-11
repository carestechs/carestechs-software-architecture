---
category: react
stack: react
status: Active
requires:
  - adrs/react/functional-components.md
conflicts_with: []
last_reviewed: 2026-10-11
---

# Installable PWA Client

## Decision
The React client ships as an installable progressive web app: a web manifest, a service worker that caches the application shell and nothing else by default, and the Web Push subscription lifecycle (ask, register with the API, renew, remove). It is served from the API's origin. No app store, no wrapper, one codebase for desktop and phone.

## Rationale
- A phone client for an internal or personal tool does not justify native apps or store accounts. An installed PWA gives a home-screen icon, a standalone window, background notifications through Web Push (installed PWAs on iOS since 16.4, Android and desktop browsers) and an offline shell.
- One origin with the API keeps the service-worker scope, cookies and push registration simple, which is why this pairs with `spa-served-by-api`.
- Alternatives considered: a native or cross-platform app (rejected; two more builds and store processes for one user base), a wrapper such as Capacitor (viable later for native APIs the web lacks), a plain SPA without a manifest (rejected; no install, no push).

## Constraints (non-negotiable for AI)
- The service worker caches the application shell (the built bundle, `index.html`, icons) and MUST NOT cache API responses by default: data comes from the API through the client's query cache, so a stale shell never shows stale data as fresh.
- `index.html` is fetched network-first with the cached copy as fallback; hashed assets are cache-first. A new worker activates on the next load and tells the user; NEVER silently keep an old bundle for days.
- Push permission is requested from a user action with a stated reason, NEVER on first load. The subscription is sent to the API with the device's identity and removed on logout or device removal.
- A push message wakes the client to fetch. The notification shows a title and one short line, NEVER sensitive content; clicking it opens the relevant screen through the client's router.
- The manifest declares `display: standalone`, maskable icons at 192 and 512 pixels, `start_url` and a theme colour. Install prompts follow the platform; nothing custom on iOS.
- Everything the client does when installed MUST also work in a normal browser tab. Install is an enhancement, never a requirement.
