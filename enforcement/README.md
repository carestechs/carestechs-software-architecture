# Enforcement

Ready-to-copy lint and analyzer configurations that turn machine-checkable ADR constraints into build-time errors. An ADR constraint that a linter enforces cannot silently regress — neither through an AI agent nor a human.

## Three tiers of enforcement

| Tier | Meaning |
|------|---------|
| **Enforced** | A lint/analyzer rule fails the build on violation. Listed in the mapping tables below. |
| **Partially enforced** | Tooling catches the common shape of the violation, but not every variant (noted per rule). |
| **Prompt-only** | Not mechanically checkable (architecture boundaries, mapping locations, semantic rules). These rely on the compiled CLAUDE.md constraints and code review. Everything not listed below is this tier. |

The configs are starting points aligned with the catalog — projects may extend them, but MUST NOT weaken a rule that backs an ADR constraint.

## .NET (`enforcement/dotnet/`)

Adoption: copy `Directory.Build.props` and `BannedSymbols.txt` next to the solution file, and merge `.editorconfig` into the solution's `.editorconfig`.

| ADR constraint | Rule | File |
|----------------|------|------|
| `adrs/dotnet/async-all-the-way.md` — never block with `.Result` / `.Wait()` | Banned symbols `Task<T>.Result`, `Task.Wait` | `BannedSymbols.txt` |
| `adrs/dotnet/async-all-the-way.md` — no fire-and-forget tasks | `CS4014` → error | `.editorconfig` |
| `adrs/dotnet/async-all-the-way.md` — no sync-over-async in async methods | `CA1849` → error (partial: catches known sync counterparts) | `.editorconfig` |
| `adrs/dotnet/async-all-the-way.md` — forward `CancellationToken` | `CA2016` → warning | `.editorconfig` |
| `adrs/dotnet/structured-logging.md` — never `Console.WriteLine` | Banned type `System.Console` | `BannedSymbols.txt` |
| `adrs/database/timestamptz-always.md` — never `DateTime.Now` | Banned symbol `DateTime.Now` | `BannedSymbols.txt` |

## Python (`enforcement/python/`)

Adoption: copy `ruff.toml` to the repo root, or merge its `[lint]` section into `[tool.ruff.lint]` in `pyproject.toml`. Run `ruff check` in CI.

| ADR constraint | Rule group | Notes |
|----------------|------------|-------|
| `adrs/python/structured-logging.md` — never `print()` for diagnostics | `T20` (e.g., `T201`) | |
| `adrs/python/structured-logging.md` — no f-strings/`.format()` in log calls | `G` (e.g., `G004`) | |
| `adrs/python/async-all-the-way.md` — no blocking calls inside `async def` | `ASYNC` | Partial: catches known blocking APIs (`requests`, `time.sleep`, sync subprocess) |
| `adrs/database/timestamptz-always.md` — no naive datetimes | `DTZ` (e.g., `DTZ005`) | |

## TypeScript (`enforcement/typescript/`)

Adoption: copy `eslint.config.mjs` to the repo root (`npm i -D eslint typescript-eslint eslint-plugin-import`) and extend `tsconfig.base.json` from the project's `tsconfig.json`. Scoped to the TypeScript CLI stack; a React app adopting `import/no-default-export` adds an override permitting default exports in route-page files per `adrs/react/functional-components.md`.

| ADR constraint | Rule | File |
|----------------|------|------|
| `adrs/typescript/strict-typescript.md` — `strict` + `noUncheckedIndexedAccess` | compiler options | `tsconfig.base.json` |
| `adrs/typescript/strict-typescript.md` — no `any` | `@typescript-eslint/no-explicit-any` | `eslint.config.mjs` |
| `adrs/typescript/strict-typescript.md` — no `@ts-ignore` / `@ts-expect-error` | `@typescript-eslint/ban-ts-comment` (all directives) | `eslint.config.mjs` |
| `adrs/typescript/named-exports.md` — no default exports | `import/no-default-export` | `eslint.config.mjs` |

## Keeping this honest

- When adding a new ADR constraint, check whether a rule can enforce it; if yes, add the rule here and a row in the table (see the CONTRIBUTING checklist).
- The catalog validator checks that every `adrs/...` path referenced in this directory exists, so renames cannot silently orphan a mapping.
