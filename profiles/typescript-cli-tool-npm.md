# Stack Profile: TypeScript CLI Tool (npm)

**Status:** Active
**Assumes:** Node.js 20+, TypeScript 5.5+, npm 10+, Vitest 2+

## Overview

A curated set of ADRs for building TypeScript-based CLI developer tools distributed via npm. This profile covers tools that run locally (terminal) and/or in CI/CD (GitHub Actions) — such as code analyzers, linters, generators, and workflow automations. The architecture separates a core engine from thin delivery adapters, enabling the same logic to power a CLI, a GitHub Action, and future surfaces (IDE plugins, other CI systems) without duplication. This is the standard stack for developer tooling that ships as an npm package.

---

## Solution Structure

```
my-tool/
├── package.json                        # Package metadata, bin, engines, scripts
├── tsconfig.json                       # TypeScript strict config
├── vitest.config.ts                    # Test runner configuration
├── .eslintrc.cjs                       # Linting rules
├── .prettierrc                         # Formatting rules
├── .env.example                        # Environment variable template
├── action.yml                          # GitHub Action metadata (if applicable)
│
├── src/
│   ├── index.ts                        # Package entry point (library API)
│   ├── cli.ts                          # CLI entry point (bin target, #!/usr/bin/env node)
│   │
│   ├── core/                           # Core engine (framework-agnostic)
│   │   ├── index.ts                    # Engine entry point: runTool(options): Promise<Result>
│   │   │
│   │   ├── config/                     # Config loading and validation
│   │   │   ├── index.ts
│   │   │   ├── config-loader.ts        # YAML parsing, Zod validation
│   │   │   ├── config-loader.test.ts
│   │   │   └── config-schema.ts        # Zod schema → inferred types
│   │   │
│   │   ├── [component-a]/              # Domain component (e.g., analyzer, resolver)
│   │   │   ├── index.ts
│   │   │   ├── [component].ts
│   │   │   └── [component].test.ts
│   │   │
│   │   └── [component-b]/              # Another domain component
│   │       ├── index.ts
│   │       ├── [component].ts
│   │       └── [component].test.ts
│   │
│   ├── adapters/                       # Delivery adapters (thin I/O layers)
│   │   ├── cli/                        # CLI adapter
│   │   │   ├── index.ts
│   │   │   ├── commands.ts             # Command definitions (Commander/yargs)
│   │   │   └── formatter.ts            # Terminal output formatting
│   │   │
│   │   └── github/                     # GitHub Action adapter
│   │       ├── index.ts
│   │       ├── action-runner.ts        # Reads event payload, calls core, posts results
│   │       └── github-client.ts        # Octokit wrapper for PR comments, status checks
│   │
│   └── types/                          # Shared type definitions
│       ├── index.ts
│       ├── config.ts                   # Config types (derived from Zod schemas)
│       ├── result.ts                   # Result/output types
│       └── errors.ts                   # Custom error classes
│
├── tests/
│   └── integration/                    # End-to-end tests (core with mocked boundaries)
│       ├── full-run.test.ts
│       └── fixtures/                   # Test config files, sample inputs
│
├── dist/                               # Compiled output (gitignored)
└── .github/
    └── workflows/
        └── ci.yml                      # Lint, typecheck, test, build
```

---

## Required (core to this stack — do not omit)

These ADRs define the fundamental architecture. Removing any of them breaks the coherence of the stack.

<!-- generated from profiles/profiles.toml — edit the manifest and run scripts/generate_profiles.py -->
| ADR | Summary | Depends On |
|-----|---------|-------------|
| `adrs/typescript/strict-typescript.md` | TypeScript strict mode, no `any`, no `@ts-ignore`. `unknown` + type guards for untyped data. | — |
| `adrs/typescript/named-exports.md` | Named exports only, no default exports. Barrel `index.ts` per component. | — |
| `adrs/typescript/functional-composition.md` | Plain functions and objects over classes. No inheritance chains. Classes only for custom Errors. | — |
| `adrs/typescript/types-at-boundary.md` | `interface` for component contracts, `type` for data shapes. All data types JSON-serializable. | `strict-typescript` |
| `adrs/typescript/core-adapter-pattern.md` | Core engine is framework-agnostic. Adapters handle I/O. Core never imports adapter code. | `strict-typescript` |
| `adrs/typescript/vitest-colocated.md` | Vitest framework, `*.test.ts` co-located with source. Mock only at system boundaries. | `strict-typescript` |
| `adrs/deployment/npm-cli-package.md` | npm distribution with `bin` field, `npx` support, `engines` field, `dist/` build output. | `strict-typescript` |
| `adrs/deployment/env-connection-urls.md` | All config via env vars. External service credentials via env vars. `.env.example` required. | — |

## Recommended (strong defaults — can be swapped with noted alternatives)

These are battle-tested defaults. You can swap them, but you should have a good reason.

<!-- generated from profiles/profiles.toml — edit the manifest and run scripts/generate_profiles.py -->
| ADR | Summary | Alternative |
|-----|---------|-------------|
| `adrs/deployment/zod-config-validation.md` | Zod for runtime validation of config files, env vars, and external input. Types derived from schemas. | io-ts (steeper learning curve), manual type guards (verbose) |
| `adrs/deployment/github-action-composite.md` | GitHub Action as composite action running Node.js. Same core engine as CLI. | JavaScript action with `@vercel/ncc` bundle (single file, more complex build) |
| `adrs/deployment/github-actions-ci.md` | CI gate on every push/PR: lint (enforcement configs), typecheck, tests, build. Deploys are separate workflows. | GitLab CI / Azure DevOps (if repos move) |

## Optional (pick based on project needs)

These address specific concerns that not every CLI tool has.

<!-- generated from profiles/profiles.toml — edit the manifest and run scripts/generate_profiles.py -->
| ADR | Summary | When to Include |
|-----|---------|-------------|
| `adrs/ai/claude-agent-sdk.md` | Claude Agent SDK for AI-powered analysis. Direct integration, abstract later. | Tools that use LLM-powered analysis, generation, or reasoning |

---

## Key Cross-Cutting Concerns

When using this stack, these patterns emerge from the combination of ADRs:

- **Config-as-code:** A single YAML config file in the repo root, validated at startup with Zod, typed throughout. The config type is the single source of truth — derived from the Zod schema, not defined separately.
- **Exit code convention:** `0` = success, `1` = expected failure (tool found issues), `2` = runtime error (bad config, API failure). Adapters map the core's typed result to the appropriate exit code.
- **Dual surface parity:** Same engine, same config, same types, same behavior — CLI and GitHub Action are thin adapters over the same `runTool()` entry point. Adding a new adapter does not require touching the core.
- **No runtime state:** Everything is transient per invocation. No database, no persistence between runs, no background processes.
- **Environment parity:** Same code works locally (developer sets env vars or uses `.env`) and in CI (GitHub Actions provides secrets as env vars). The tool doesn't know or care which environment it's in.
- **System boundary mocking:** Tests mock at exactly four boundaries — external APIs, AI/LLM calls, git/child_process, and filesystem — everything else is tested through real function calls.
- **Lazy imports for fast startup:** The CLI entry point lazy-imports heavy dependencies (AI SDKs, HTTP clients) so that `--help`, `--version`, and lightweight commands respond instantly.

## Development Workflow

- **Local development first:** Get the CLI running with `ts-node` or `tsx` before worrying about the GitHub Action. The core engine is the same — if it works locally, it works in CI.
- **Test-driven config:** Write tests for config validation first (valid configs, invalid configs, edge cases). The config schema is the foundation everything else builds on.
- **Integration tests with fixtures:** End-to-end tests use fixture files (sample configs, sample inputs) and mock only external boundaries. They exercise the full core pipeline.
- **Dogfooding:** Use the tool on its own codebase as soon as it's functional. The project's own config file is a live integration test.

### Local Development Commands

```bash
# Install dependencies
npm install

# Run CLI in dev mode (via ts-node/tsx)
npx tsx src/cli.ts check --help
npx tsx src/cli.ts check ./src

# Run tests
npm test

# Run tests in watch mode
npm run test:watch

# Type check
npm run typecheck

# Lint
npm run lint

# Build
npm run build

# Test the built CLI
node dist/cli.js check --help
```

### Publishing

```bash
# Bump version
npm version patch|minor|major

# Build and publish
npm run build
npm publish

# Test npx invocation
npx my-tool check --help
```
