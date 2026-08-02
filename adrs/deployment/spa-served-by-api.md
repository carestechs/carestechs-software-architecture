---
category: deployment
stack: any
status: Active
requires:
  - adrs/deployment/docker-multi-stage-builds.md
conflicts_with:
  - adrs/deployment/nginx-spa-proxy.md
last_reviewed: 2026-08-02
---

# SPA Served by the API Host

## Decision
The SPA's production bundle is built in a Node stage of the API's multi-stage Dockerfile and copied into the API image (`wwwroot/` on ASP.NET Core, a static-files mount elsewhere). The API serves it: static files plus an SPA fallback to `index.html`, registered after the API routes. One application container, one origin — no separate web server container and no CORS configuration.

## Rationale
- At single-server scale, a dedicated nginx container for the SPA is a second deployable, a second config surface, and a proxy hop that buys nothing the API's static-file middleware doesn't already provide.
- Same-origin is a security simplification, not just an operational one: no CORS policy to get wrong, cookies stay first-party (`SameSite=Strict` refresh cookies work unmodified).
- Alternatives considered: nginx serving the SPA and proxying `/api` (the sibling `nginx-spa-proxy` ADR — the right choice the moment frontend and backend scale or deploy independently), a CDN/S3 bucket for the SPA (rejected at this rung — separate origin brings CORS and a deploy pipeline), serving the SPA from the frontend dev server in production (never acceptable).
- The build stays honest: the same multi-stage Dockerfile discipline applies — Node builds the bundle, the runtime image contains only the API plus static assets.

## Constraints (non-negotiable for AI)
- The SPA fallback MUST be registered AFTER API routes, and API paths (`/api/*`, `/health`) MUST NEVER fall back to `index.html` — an unknown API route returns its Problem Details 404, not HTML.
- Hashed bundle assets get long-lived immutable cache headers; `index.html` MUST be served with no-cache semantics so deploys take effect on refresh.
- NEVER add CORS configuration under this ADR — the SPA and API share one origin by construction. A CORS policy appearing in the codebase signals drift toward a split this ADR does not cover.
- The production image is self-contained: the SPA bundle is built inside the Dockerfile's Node stage — NEVER copied in from a developer machine.
- Development uses the SPA dev server proxying to the API (same paths as production); the dev server is NEVER part of any deployed image.
