# npm Package Distribution with CLI Binary

**Category:** deployment
**Status:** Active
**Requires:** `adrs/typescript/strict-typescript.md`
**Conflicts with:** —

## Decision

The tool is distributed as an npm package with a CLI binary entry point. Users can run it via `npx` (zero-install), install it globally, or add it as a project dev dependency. The package compiles TypeScript to `dist/` and declares its binary in `package.json`.

## Rationale

- npm is the standard distribution channel for JavaScript/TypeScript developer tools. `npx` provides zero-install execution, which is critical for CI/CD and first-time users. The barrier to trying the tool is a single command.
- Alternatives considered: standalone binary via `pkg` or `bun compile` (rejected — adds build complexity, loses the npm ecosystem for updates/versioning), Docker image only (rejected — too heavy for a CLI tool, doesn't work in all CI environments), GitHub release binaries (rejected — no standard install mechanism, manual updates).
- Publishing to npm gives automatic versioning and git tags via `npm version`, and dependency resolution for consumers who want to pin a specific version (changelogs are a separate concern — e.g., changesets or release notes).

## Constraints (non-negotiable for AI)

- `package.json` MUST declare a `"bin"` field mapping the CLI command name to the compiled entry point (e.g., `"bin": { "code-review-agent": "./dist/cli.js" }`).
- The compiled `dist/cli.js` entry point MUST start with `#!/usr/bin/env node`.
- `package.json` MUST declare an `"engines"` field specifying the minimum Node.js version.
- `package.json` MUST declare `"files"` to include only `dist/`, `README.md`, `LICENSE`, and (when applicable) `action.yml` — NEVER publish `src/`, `tests/`, or config files.
- The `dist/` directory MUST be gitignored — it is a build artifact, not source.
- `npx <package-name>` MUST work without prior installation — the package entry point must be self-contained.
- Type declarations (`.d.ts`) MUST be included in the published package for library consumers.
