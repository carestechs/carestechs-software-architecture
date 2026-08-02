# Golden Skeleton: dotnet-angular-simple-monolith-single-server

A minimal, **buildable** instance of `profiles/dotnet-angular-simple-monolith-single-server.md` —
the catalog's entry rung. Its job is to prove, in CI, that the profile's ADRs compose into a
system that builds, tests, and ships as ONE deployable. If a catalog change breaks this
skeleton, the change — not the skeleton — is suspect.

## What it demonstrates

| Area | ADRs exercised |
|------|----------------|
| ONE web project: `Features/Catalog` + `Features/Identity` folders, one `AppDbContext`, one EF migration (`products`, `users`, `refresh_tokens` — snake_case) | `dotnet/single-project-monolith`, `database/snake-case-naming` |
| Services behind interfaces, thin controllers, DTOs at the boundary — the graduation discipline held in full | `dotnet/service-layer-logic`, `dotnet/dto-at-boundary` |
| In-process background job: product creation enqueues to a bounded `Channel<T>`; the hosted `JobRunner` (scope per job) stamps `searchIndexedAt` — proven end to end by a test that polls until the real `BackgroundService` ran | `dotnet/in-process-background-jobs` |
| SPA served by the API: Angular bundle built in the Dockerfile's Node stage into `wwwroot`; fallback registered AFTER API routes — `/some/spa/route` returns HTML, `/api/nope` stays a Problem Details 404 (both tested) | `deployment/spa-served-by-api` |
| `{ data, meta }` envelope, Problem Details via `IExceptionHandler`, camelCase JSON | `api/rest-envelope`, `dotnet/rfc7807-errors` |
| JWT auth with the full discipline: 15-min HS256 (alg allowlist, iss/aud, 60s skew), refresh rotation with family revocation on reuse, httpOnly `SameSite=Strict` cookie, CSRF header guard, logout revoking the family, deny-by-default `FallbackPolicy` | `api/jwt-bearer-auth`, `api/role-based-authorization` |
| `Guid.CreateVersion7()` PKs, `DateTimeOffset.UtcNow`, `ILogger` JSON console + request-ID scopes, BannedApiAnalyzers as errors (0-warning build) | `database/uuid-primary-keys`, `database/timestamptz-always`, `dotnet/structured-logging`, enforcement layer |
| One xUnit (v3) test project, `WebApplicationFactory` against real PostgreSQL — hosted services included | `dotnet/xunit-per-module-tests` (single-project shape) |
| Angular 20 standalone + signals + Tailwind v4, dev server proxying `/api` | `angular/standalone-components`, `angular/signals-state`, `angular/tailwind-no-css` |
| One app container + postgres via compose on one server; dev compose is infra-only | `deployment/local-dev-compose`, `deployment/container-per-process`, `deployment/docker-multi-stage-builds`, `deployment/env-connection-urls` |
| CI: dotnet build+test (PostgreSQL service) + the full image build proving the SPA lands in `wwwroot` | `deployment/github-actions-ci` |

## Run it locally

```bash
# infrastructure (postgres only — the app runs on the host)
docker compose up -d

# apply the EF migration, run the app (http://localhost:5000)
dotnet ef database update --project src/MyApp.Web
dotnet run --project src/MyApp.Web
# dev users seed automatically in Development:
#   admin@example.com / Admin123!, agent@example.com / Agent123!

# tests (uses TEST_DATABASE_URL, defaults to localhost app_test — create it first:
#   docker compose exec postgres createdb -U postgres app_test)
dotnet test

# frontend dev server (separate terminal; proxies /api to :5000)
cd client && npm install && npm start
```

## Deploy to one server

```bash
JWT_SECRET=<random-32+bytes> docker compose -f docker-compose.prod.yml up -d --build
# one app container (API + SPA) + postgres with a named volume; port 80
```

## Endpoint access matrix

| Endpoint | Access |
|----------|--------|
| `POST /api/auth/login` | anonymous |
| `POST /api/auth/refresh`, `POST /api/auth/logout` | refresh cookie + `X-Requested-With` header |
| `GET /api/products`, `GET /api/products/{id}` | anonymous — public catalog, deliberate `[AllowAnonymous]` |
| `POST /api/products` | role `admin` |
| `GET /health` | anonymous |
| any non-API route | anonymous — SPA fallback to `index.html` |

## Deliberately not demonstrated (yet)

- **Hangfire for must-survive jobs** — the channel job here is tolerable-loss by design (search-index
  sync, reconciled by re-indexing); the ADR's persistent-store path is documented, not built.
- **Offset pagination** (Optional tier) — the list endpoint returns all rows with `meta.totalCount`.
- **A login UI** — the Angular client shows the public catalog; auth is proven at the API layer.

When this app outgrows the rung, the graduation path is the modular-monolith profile — see the
profile overview. This skeleton is held to the same constraints it demonstrates.
