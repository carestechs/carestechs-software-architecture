# GitHub Action as Composite Action (Node.js)

**Category:** deployment
**Status:** Active
**Requires:** `adrs/deployment/npm-cli-package.md`
**Conflicts with:** —

## Decision

The tool ships as a GitHub Action using the composite action type that runs Node.js. The action is defined by an `action.yml` in the repository root with typed inputs, outputs, and a `runs` section that invokes the compiled CLI. The same core engine powers both the standalone CLI and the Action.

## Rationale

- Composite actions are simpler than Docker-based actions — no image build, no registry, faster startup. Since the tool is already a Node.js CLI, the action just invokes it with the right arguments.
- Alternatives considered: Docker action (rejected — adds 10-30s image pull overhead per run, requires maintaining a Docker image), JavaScript action with `@vercel/ncc` bundle (viable alternative — trades action.yml simplicity for a single-file bundle; can migrate to this if composite proves limiting).
- The action reuses the exact same compiled code as the CLI. No separate codebase, no behavior drift between local and CI usage.

## Constraints (non-negotiable for AI)

- `action.yml` MUST live in the repository root.
- The action MUST use `runs.using: "composite"` with `runs.steps` that set up Node.js (`actions/setup-node`) and invoke the compiled CLI (`npx <package>` or `node dist/cli.js`).
- All action inputs MUST be declared with `description` and `required` fields in `action.yml`.
- All action outputs MUST be declared and set via `$GITHUB_OUTPUT`.
- Structured outputs MUST be written to `$GITHUB_OUTPUT` by the composite steps, and workflow-facing annotations use GitHub's workflow commands (e.g., `::error::`); the CLI itself keeps its normal stdout behavior from `npm-cli-package.md`. (`@actions/core` applies only if the action is later migrated to a JavaScript action.)
- The action MUST reuse the same core engine as the CLI — NEVER duplicate logic between the action adapter and the CLI adapter.
- Secrets (API keys, tokens) MUST be passed as inputs, NEVER hardcoded or read from the environment implicitly.
