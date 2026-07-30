---
category: typescript
stack: typescript
status: Active
requires:
  - adrs/typescript/strict-typescript.md
conflicts_with: []
last_reviewed: 2026-07-29
---

# Core Engine with Delivery Adapters

## Decision

The application is structured as a core engine (business logic, data processing, orchestration) with thin delivery adapters (CLI, GitHub Action, future: IDE plugin, other CI systems). The core engine is framework-agnostic and never imports adapter code. Adapters translate between the external world and the core's typed inputs/outputs.

## Rationale

- CLI tools and developer tools often need to run in multiple contexts — locally, in CI/CD, in IDEs, as library functions. If business logic is coupled to one delivery mechanism, adding a second requires a rewrite or ugly abstractions.
- The core engine defines a single entry-point function that accepts typed options and returns typed results. Each adapter is responsible only for: (1) collecting input from its environment, (2) calling the core, (3) formatting the core's output for its medium.
- Alternatives considered: monolithic CLI that also handles CI (rejected — CI needs different I/O patterns), plugin architecture (rejected — premature for v1; the adapter pattern is simpler and achieves the same decoupling).

## Constraints (non-negotiable for AI)

- The core engine MUST expose a single entry-point function (e.g., `runTool(options): Promise<Result>`) that accepts typed input and returns typed output.
- The core engine MUST NOT import any adapter-specific code (no `@actions/core`, no CLI framework, no Express/Hono).
- Adapters MUST NOT contain business logic — they collect input, call the core, and format output.
- All data flowing between adapters and the core MUST use shared types from a `types/` directory.
- Adding a new adapter MUST NOT require changes to the core engine or existing adapters.
- Adapters MUST handle their own error presentation (e.g., CLI prints to stderr, GitHub Action posts a comment) — the core returns typed errors, not formatted messages.
