# Stack Profile: TypeScript AI Agent CLI Tool (npm)

**Status:** Active
**Assumes:** Node.js 20+, TypeScript 5.5+, npm 10+, Vitest 2+, Claude Agent SDK (`@anthropic-ai/claude-agent-sdk`)

## Overview

A curated set of ADRs for building TypeScript-based AI agent CLI tools distributed via npm. This profile extends the base `TypeScript CLI Tool (npm)` profile with ADRs for AI-powered analysis using the Claude Agent SDK. The architecture adds an AI orchestrator component to the core engine that handles all LLM interaction, tool definition, and structured output parsing. Typical tools in this category include code review agents, documentation generators, test generators, and codebase analyzers. This is the standard stack for AI-powered developer tools that ship as npm packages.

---

## Solution Structure

```
my-ai-tool/
├── package.json                        # Package metadata, bin, engines, scripts
├── tsconfig.json                       # TypeScript strict config
├── vitest.config.ts                    # Test runner configuration
├── .eslintrc.cjs                       # Linting rules
├── .prettierrc                         # Formatting rules
├── .env.example                        # ANTHROPIC_API_KEY, GITHUB_TOKEN, etc.
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
│   │   │   ├── config-loader.ts
│   │   │   ├── config-loader.test.ts
│   │   │   └── config-schema.ts        # Zod schema → inferred types (includes AI config)
│   │   │
│   │   ├── [domain-component]/         # Domain-specific component (e.g., doc resolver, diff provider)
│   │   │   ├── index.ts
│   │   │   ├── [component].ts
│   │   │   └── [component].test.ts
│   │   │
│   │   ├── ai/                         # AI orchestrator (all LLM interaction)
│   │   │   ├── index.ts
│   │   │   ├── orchestrator.ts         # Assembles context, calls Claude, parses response
│   │   │   ├── orchestrator.test.ts
│   │   │   ├── prompts.ts              # Prompt templates and construction
│   │   │   ├── output-schema.ts        # Zod schemas for AI response validation
│   │   │   └── tools/                  # Tool definitions for Claude Agent SDK
│   │   │       ├── index.ts
│   │   │       └── [tool-name].ts      # Individual tool definitions
│   │   │
│   │   └── output/                     # Result formatting
│   │       ├── index.ts
│   │       ├── formatter.ts
│   │       └── formatter.test.ts
│   │
│   ├── adapters/                       # Delivery adapters (thin I/O layers)
│   │   ├── cli/                        # CLI adapter
│   │   │   ├── index.ts
│   │   │   ├── commands.ts
│   │   │   └── formatter.ts            # Terminal output (human-readable + JSON)
│   │   │
│   │   └── github/                     # GitHub Action adapter
│   │       ├── index.ts
│   │       ├── action-runner.ts
│   │       └── github-client.ts        # PR comments, status checks via Octokit
│   │
│   └── types/                          # Shared type definitions
│       ├── index.ts
│       ├── config.ts                   # Config types (includes AIConfig)
│       ├── result.ts                   # Result/output types
│       ├── ai.ts                       # AI-specific types (context, tool results)
│       └── errors.ts                   # Custom error classes (includes AIError)
│
├── tests/
│   └── integration/                    # End-to-end tests (core with mocked AI boundary)
│       ├── full-run.test.ts
│       └── fixtures/
│           ├── sample-config.yml
│           ├── sample-input/           # Sample files/data for the tool to analyze
│           └── mock-ai-responses/      # Canned AI responses for deterministic tests
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
| `adrs/deployment/env-connection-urls.md` | All config via env vars. API keys and tokens via env vars. `.env.example` required. | — |
| `adrs/deployment/zod-config-validation.md` | Zod for runtime validation of config files, env vars, and AI responses. Types from schemas. | `strict-typescript` |
| `adrs/ai/claude-agent-sdk.md` | Claude Agent SDK for AI analysis. Single orchestrator component. AI responses validated as untrusted. | `strict-typescript` |

## Recommended (strong defaults — can be swapped with noted alternatives)

These are battle-tested defaults. You can swap them, but you should have a good reason.

<!-- generated from profiles/profiles.toml — edit the manifest and run scripts/generate_profiles.py -->
| ADR | Summary | Alternative |
|-----|---------|-------------|
| `adrs/deployment/github-action-composite.md` | GitHub Action as composite action running Node.js. Same core engine as CLI. | JavaScript action with `@vercel/ncc` bundle (single file, more complex build) |
| `adrs/deployment/github-actions-ci.md` | CI gate on every push/PR: lint (enforcement configs), typecheck, tests, build. Deploys are separate workflows. | GitLab CI / Azure DevOps (if repos move) |

## Optional (pick based on project needs)

These address specific concerns that not every AI tool has.

<!-- generated from profiles/profiles.toml — edit the manifest and run scripts/generate_profiles.py -->
| ADR | Summary | When to Include |
|-----|---------|-------------|

---

## Key Cross-Cutting Concerns

When using this stack, these patterns emerge from the combination of ADRs. All concerns from the base `TypeScript CLI Tool (npm)` profile apply, plus:

- **AI orchestrator isolation:** All Claude Agent SDK interaction is encapsulated in `src/core/ai/`. No other component imports `@anthropic-ai/claude-agent-sdk`. If the AI backend changes, only this directory is affected.
- **AI responses as untrusted input:** The orchestrator calls Claude, receives a response, and validates it through a Zod schema before returning typed data to the rest of the engine. Malformed AI output produces a typed error, not a runtime crash.
- **Prompt construction is code:** Prompt templates live in `prompts.ts` as functions that accept typed context and return strings. They are version-controlled, testable, and reviewable — not hidden in config files or databases.
- **Tool definitions as declarations:** Claude Agent SDK tools are declared as typed objects with name, description, and parameter schema. Tool implementations delegate to existing core functions — no business logic in tool handlers.
- **Cost awareness:** The orchestrator tracks input/output token counts per invocation and exposes them in the result. This enables cost monitoring without requiring a separate telemetry system.
- **Deterministic testing with canned responses:** Integration tests use fixture files containing canned AI responses. The AI SDK boundary is mocked to return these fixtures, making tests fast, deterministic, and free of API costs.
- **Model configurability:** The Claude model identifier is always read from config or env var, never hardcoded. This allows switching between models (Haiku for fast/cheap dev testing, Sonnet/Opus for production) without code changes.
- **Graceful AI failure:** If the Claude API is unreachable, rate-limited, or returns an error, the tool reports a clear error and exits with code 2 (runtime error). It never produces partial or fabricated results.
- **Config-as-code (extended):** The config file includes an `ai` section for model selection and token limits. AI settings follow the same Zod validation and type derivation as all other config.
- **Exit code convention:** Same as base profile — `0` success, `1` findings/issues, `2` runtime error. AI failures (API errors, invalid responses) are always exit code `2`.
- **Lazy AI import:** The Claude Agent SDK is imported lazily — only when the AI orchestrator is actually invoked. Commands like `--help`, `init`, and `validate` never load the SDK.

## Development Workflow

All guidance from the base `TypeScript CLI Tool (npm)` profile applies, plus:

- **Mock AI during development:** Use canned response fixtures for most development work. Only call the real Claude API for prompt tuning and integration verification.
- **Prompt iteration loop:** When tuning prompts, use a lightweight model (Haiku) for fast iteration, then verify with the target model (Sonnet/Opus) before committing.
- **Cost guardrails:** Set conservative `maxTokens` in the dev config. Log token usage per run to catch prompt regressions that inflate cost.
- **Test the validation boundary:** Write tests that feed malformed AI responses (missing fields, wrong types, unexpected structure) to the Zod validation layer. The orchestrator must handle these gracefully.

### Local Development Commands

```bash
# Install dependencies
npm install

# Set up environment
cp .env.example .env
# Edit .env: set ANTHROPIC_API_KEY, optionally GITHUB_TOKEN

# Run CLI in dev mode (via tsx)
npx tsx src/cli.ts check --help
npx tsx src/cli.ts check ./src

# Run with verbose output (shows AI token usage)
npx tsx src/cli.ts check ./src --verbose

# Run tests (uses canned AI responses, no API key needed)
npm test

# Run tests in watch mode
npm run test:watch

# Type check
npm run typecheck

# Lint
npm run lint

# Build
npm run build

# Test the built CLI against the real API
ANTHROPIC_API_KEY=sk-... node dist/cli.js check ./src
```

### Publishing

```bash
# Bump version
npm version patch|minor|major

# Build and publish
npm run build
npm publish

# Test npx invocation
npx my-ai-tool check --help
```
