# One Process Type per Container

**Category:** deployment
**Status:** Active
**Requires:** `adrs/deployment/docker-multi-stage-builds.md`
**Conflicts with:** —

## Decision
Each process type runs in its own container: the API server, background workers, and the frontend are separate services defined in Docker Compose (or equivalent orchestrator). Containers that share the same runtime (e.g., API and worker) reuse the same Docker image but override the command. Each container defines its own health check appropriate to its process type.

## Rationale
- Separating process types into individual containers enables independent scaling (e.g., scale workers without scaling the API), independent restarts (a crashing worker does not take down the API), and clearer resource monitoring per process type.
- Alternatives considered: running all processes in a single container with a process manager like supervisord (rejected — conflates failure domains, makes scaling impossible, harder to debug), running workers as background threads inside the API process (rejected — tasks lost on process restart, no horizontal scaling, GIL contention in Python).
- Reusing the same image for API and worker containers avoids maintaining separate Dockerfiles for the same codebase. The only difference is the entrypoint command (e.g., `uvicorn` vs `celery worker`, or `dotnet run` vs a background service).
- Per-container health checks allow the orchestrator to detect and restart unhealthy processes independently.

## Constraints (non-negotiable for AI)
- The API server, background workers, and frontend MUST run as separate containers. NEVER run multiple process types inside a single container.
- Containers sharing the same codebase (e.g., API and worker) MUST use the same Docker image with different `command` overrides in the Compose file. NEVER maintain separate Dockerfiles for the same codebase.
- Every container MUST define a `healthcheck` in the Compose file or orchestrator manifest.
- API containers MUST use an HTTP health endpoint (e.g., `GET /health`) for their health check.
- Database and cache containers MUST use native CLI health checks (e.g., `pg_isready`, `redis-cli ping`).
- All containers MUST set `restart: unless-stopped` (Compose) or equivalent restart policy. NEVER use `restart: always` (prevents intentional stops) or omit the restart policy.
- Worker containers MUST set concurrency explicitly via command-line flags (e.g., `celery worker -c 4`). NEVER rely on library defaults for production concurrency.
