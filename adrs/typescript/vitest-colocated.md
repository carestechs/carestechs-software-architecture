---
category: typescript
stack: typescript
status: Active
requires:
  - adrs/typescript/strict-typescript.md
conflicts_with: []
last_reviewed: 2026-07-29
verify_against:
  - Vitest 2
---

# Vitest with Co-Located Test Files

## Decision

Use Vitest as the test framework. Test files are co-located next to their source files using the `*.test.ts` naming convention. Tests follow the Arrange-Act-Assert pattern and mock only at system boundaries.

## Rationale

- Vitest is native to the Vite/TypeScript ecosystem — zero-config for TypeScript, ESM-first, fast watch mode, compatible with the Jest API developers already know. It eliminates the transform/config overhead that Jest requires for TypeScript projects.
- Co-located tests reduce navigation friction: `config-loader.ts` and `config-loader.test.ts` live side by side. When a file moves, its test moves with it. When a file is deleted, the orphan test is obvious.
- Mocking only at system boundaries (external APIs and SDKs, git CLI, filesystem) ensures tests verify real logic, not mocked logic. Internal function-to-function calls are tested through their natural call chains.

## Constraints (non-negotiable for AI)

- Test files MUST be named `*.test.ts` and placed next to the source file they test.
- All tests MUST follow the Arrange-Act-Assert (AAA) pattern.
- Mock ONLY at system boundaries: external API/SDK calls (e.g., LLM SDKs, VCS-host APIs), git/child_process calls, filesystem I/O.
- NEVER mock internal functions or module-to-module calls — test through the public interface.
- Each `describe` block MUST correspond to a function or logical unit, each `it` block MUST describe a single behavior.
- Integration tests (end-to-end through the core engine with mocked external boundaries) MUST live in a top-level `tests/integration/` directory.
