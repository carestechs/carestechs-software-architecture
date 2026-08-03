# Web

Angular 20 SPA (standalone components, signals, Tailwind v4) consuming the bare-DTO API.

- Dev: `npm start` — the dev server proxies `/v1` to `App.Api` on :5000 (`proxy.conf.json`).
- Production: `docker build` — multi-stage Node build into an nginx image that serves the
  bundle and proxies `/v1` to the `api` compose service (adrs/deployment/nginx-spa-proxy.md).
