# Docker Containerization with Multi-Stage Builds

**Category:** deployment
**Status:** Active
**Requires:** —
**Conflicts with:** —

## Decision
All application components are packaged as Docker images using multi-stage builds. Backend images use a slim runtime base with dependencies installed in a separate stage. Frontend images use a Node build stage to produce static assets, then copy the output to an nginx runtime stage. Build tools, dev dependencies, and source artifacts never appear in the final image.

## Rationale
- Multi-stage builds produce significantly smaller production images by discarding build-time tooling (compilers, package managers, dev dependencies) from the final layer. Smaller images mean faster pulls, reduced attack surface, and lower storage costs.
- Alternatives considered: single-stage builds (rejected — bloated images with unnecessary build tools), building outside Docker and copying artifacts in (rejected — loses reproducibility, introduces "works on my machine" issues), distroless images (viable but less debuggable — can be adopted later for hardened environments).
- A `.dockerignore` file ensures local dev artifacts (`.venv`, `node_modules`, `.git`, tests, docs) are excluded from the build context, speeding up builds and preventing accidental secret leakage.

## Constraints (non-negotiable for AI)
- Every deployable component (backend, frontend, worker) MUST have its own Dockerfile.
- All Dockerfiles MUST use multi-stage builds: a build stage for installing/compiling and a final stage with only the runtime base and production artifacts.
- Backend Dockerfiles MUST use a slim or alpine base image for the final stage (e.g., `python:3.12-slim`, `mcr.microsoft.com/dotnet/aspnet`). NEVER use full SDK or build images as the final stage.
- Frontend Dockerfiles MUST use a Node image for the build stage and an `nginx:alpine` image for the final stage. The build output (e.g., `dist/`) is copied to nginx's serving directory.
- A `.dockerignore` file MUST exist at the build context root, excluding at minimum: `.venv`, `node_modules`, `.git`, `__pycache__`, `.env`, `tests/`, `docs/`, and IDE configuration files.
- NEVER install dev dependencies in the final stage. Use `--no-dev`, `--only=production`, or equivalent flags when installing dependencies for production.
- NEVER copy the entire source tree into the final stage if only a build artifact (e.g., compiled output, static files) is needed.
