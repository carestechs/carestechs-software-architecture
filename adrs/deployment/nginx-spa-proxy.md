# Nginx Reverse Proxy for SPA Serving

**Category:** deployment
**Stack:** any
**Status:** Active
**Requires:** `adrs/deployment/docker-multi-stage-builds.md`
**Conflicts with:** —
**Last reviewed:** 2026-07-29

## Decision
The frontend is served via nginx running in a container. Nginx serves the built SPA static files and reverse-proxies API requests (e.g., `/api/`) to the backend service. Client-side routing is handled with a `try_files` fallback to `index.html`. The nginx configuration is stored in the repository alongside the frontend source.

## Rationale
- Nginx is the industry standard for serving static files and reverse proxying. It handles TLS termination, gzip compression, caching headers, and connection pooling out of the box, with minimal resource usage compared to a Node.js server serving static files.
- Alternatives considered: serving the SPA from the backend framework itself (rejected — couples frontend deployment to backend, complicates the backend's responsibility), using a Node.js server like `serve` or `express.static` (rejected — more resource-intensive, no built-in reverse proxy capabilities), using a CDN without a reverse proxy (viable for pure static sites but does not solve API proxying for same-origin requests).
- Reverse proxying API requests through nginx eliminates CORS complexity in production — the frontend and API share the same origin. CORS configuration is only needed for local development where the frontend dev server and API run on different ports.
- Storing `nginx.conf` in the repository ensures the serving configuration is versioned, reviewed, and reproducible alongside the frontend code.

## Constraints (non-negotiable for AI)
- The frontend container MUST use `nginx:alpine` as the final stage in its multi-stage Dockerfile.
- The built SPA assets MUST be copied to `/usr/share/nginx/html/` in the nginx container.
- The nginx configuration MUST include a `try_files $uri $uri/ /index.html` directive (or equivalent) to support client-side routing. NEVER let nginx return 404 for valid client-side routes.
- API requests MUST be reverse-proxied to the backend service using a `location /api/` block with `proxy_pass`. The backend service MUST be referenced by its Docker service name (e.g., `http://research-api:8000`).
- The nginx configuration MUST set `client_max_body_size` explicitly (default to `10m` unless the application's uploads require more). NEVER rely silently on nginx's 1MB default.
- A custom `nginx.conf` MUST be stored in the frontend directory (e.g., `client/nginx.conf`) and copied into the container at build time. NEVER rely on the default nginx configuration.
- The `proxy_pass` directive MUST include `proxy_set_header Host`, `X-Real-IP`, and `X-Forwarded-For` headers to preserve client information for the backend.
