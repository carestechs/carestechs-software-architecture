---
category: deployment
stack: any
status: Active
requires: []
conflicts_with: []
last_reviewed: 2026-07-30
---

# GitHub Actions CI Gate

## Decision
Every repository runs a GitHub Actions workflow on every push to the default branch and every pull request. The gate runs, at minimum: linting (the `enforcement/` configs), type checking, the full test suite, and a production build. A red check blocks merge. Deployment is a separate workflow triggered by a merge to the default branch or a tag — never by a PR build.

## Rationale
- The repositories live on GitHub; Actions is the zero-friction CI with first-class PR checks, secrets, and matrix support. Alternatives considered: GitLab CI / Azure DevOps (rejected — the repos are not there), Jenkins (rejected — self-hosted operational burden), pre-commit hooks only (rejected — trivially bypassable; the server-side gate is the enforcement point).
- The `enforcement/` lint configs only protect the catalog's constraints if something actually executes them on every change. CI is that something — for AI agents and humans alike.
- Separating CI (verify) from CD (deploy) keeps PR builds side-effect-free: a PR from any branch can never touch an environment, and deploy credentials are never exposed to PR-triggered runs.

## Constraints (non-negotiable for AI)
- A CI workflow MUST run on every push to the default branch and on every pull request.
- CI MUST run, at minimum: linting with the `enforcement/` configs, type checking, the full test suite, and a production build of every deployable artifact.
- A failing check MUST block merge. NEVER merge on red, and NEVER disable or skip a check to get to green — fix the cause.
- CI workflows MUST NOT deploy. Deployment is a separate workflow triggered by merge to the default branch or by a tag, with its own environment-scoped secrets.
- Secrets MUST come from GitHub Actions secrets (or the environment's secret store) — NEVER committed, echoed, or passed through PR-controlled inputs.
- Third-party actions MUST be version-pinned (e.g., `actions/checkout@v4`) — NEVER `@main`/`@master`/`@latest`.
- PR workflows MUST set a concurrency group with `cancel-in-progress: true` so superseded runs on the same ref are cancelled.

## Examples

**Violation — deploying from a PR build with an unpinned action:**
```yaml
on: pull_request
jobs:
  build:
    steps:
      - uses: actions/checkout@main
      - run: docker compose -f docker-compose.prod.yml up -d  # deploy inside CI
```

**Compliant:**
```yaml
on:
  push: { branches: [main] }
  pull_request:
concurrency:
  group: ci-${{ github.ref }}
  cancel-in-progress: true
jobs:
  verify:
    steps:
      - uses: actions/checkout@v4
      - run: npm run lint && npm run typecheck && npm test && npm run build
# deploy lives in a separate workflow triggered by push to main or a tag
```
