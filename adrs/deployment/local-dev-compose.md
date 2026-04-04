# Separate Dev and Prod Docker Compose

**Category:** deployment
**Status:** Active
**Requires:** `adrs/deployment/docker-multi-stage-builds.md`, `adrs/deployment/env-connection-urls.md`
**Conflicts with:** —

## Decision
The project maintains two Docker Compose files with distinct responsibilities. `docker-compose.yml` provides local development infrastructure only (database, Redis, and other backing services) — developers run the application processes directly on their host for fast iteration. `docker-compose.prod.yml` defines the application services (API, worker, frontend) and connects them to an external shared infrastructure network where databases and caches are already running. Production never bundles its own database or cache containers.

## Rationale
- Separating dev and prod Compose files enforces a clean boundary: dev Compose owns infrastructure lifecycle, prod Compose owns application lifecycle. This prevents accidental production deployments with embedded databases and makes the infrastructure topology explicit.
- Alternatives considered: a single Compose file with profiles or overrides (rejected — `docker-compose.override.yml` chains become confusing and error-prone, especially when dev needs different services than prod), no Compose at all for dev (rejected — forces developers to install and configure PostgreSQL, Redis, etc. natively), bundling infrastructure in prod Compose (rejected — production databases should be managed services or shared containers, not per-app instances).
- An external Docker network (e.g., `infra`) allows multiple applications to share the same database and cache instances in production, mirroring managed service setups and reducing resource duplication.
- Local dev runs application processes on the host (not in containers) to enable hot-reload, debugger attachment, and faster feedback loops. Only the backing services run in containers.

## Constraints (non-negotiable for AI)
- `docker-compose.yml` MUST contain only infrastructure services (database, Redis, message brokers). NEVER define application services (API, worker, frontend) in the dev Compose file.
- `docker-compose.prod.yml` MUST contain only application services. NEVER define database or cache services in the prod Compose file — these connect to shared infrastructure via environment variables.
- Production Compose MUST declare an external network for infrastructure connectivity. Application services connect to databases and caches through this shared network using Docker DNS names.
- All infrastructure services in the dev Compose MUST define health checks so application processes can wait for readiness.
- Dev infrastructure services MUST expose ports to the host (e.g., `5432:5432`, `6379:6379`) so developers can run application processes directly on the host.
- The prod Compose MUST load environment variables from a `.env` file or receive them from the orchestrator. NEVER hardcode connection strings in the Compose file.
- Infrastructure data MUST use named Docker volumes for persistence across container restarts in dev. NEVER use bind mounts for database data directories.
