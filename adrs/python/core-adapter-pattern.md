---
category: python
stack: python
family: core-adapter-pattern
status: Active
requires: []
conflicts_with:
  - adrs/typescript/core-adapter-pattern.md
last_reviewed: 2026-10-11
---

# Core Engine with Delivery Adapters (Python)

## Decision
The program is a core engine (its logic, as plain functions and dataclasses) with thin adapters for every way it meets the world: command line, daemon loop, HTTP hook, file tail, OS service. The core never imports an adapter; adapters collect input, call one typed entry point per operation, and present the result in their medium.

## Rationale
- A tool that runs as a one-shot command, as a long-running daemon and under a test harness has three input/output patterns and one logic. Coupling the logic to any one of them makes the others a rewrite.
- The core's entry points take a typed request and return a typed result, so adapters are small enough for a handful of tests each and the core is tested without a process, a socket or a file.
- Alternatives considered: one module that parses `argv`, loops and does the work (rejected; untestable without a process), a plugin system (rejected; premature, the adapter seam gives the same decoupling), an abstract base class hierarchy for adapters (rejected; a `Protocol` or a plain callable is enough in Python).
- Python variant of the TypeScript decision: the shape is identical, the mechanics (dataclasses, `typing.Protocol`, `argparse`) differ.

## Constraints (non-negotiable for AI)
- The core exposes ONE entry point per operation with a typed input and a typed output (dataclasses or TypedDicts). It MUST NOT read `sys.argv`, environment variables, stdin or sockets itself.
- The core MUST NOT import adapter modules or the I/O libraries adapters own (`argparse`, `http.server`, `urllib.request` belong to adapters; the core receives data and returns data).
- Adapters MUST NOT contain business logic: they parse, call, format, and map typed errors to their medium (exit code, HTTP status, log line).
- Shared types live in one module both sides import. Adding an adapter MUST NOT change the core or the other adapters.
- Side effects the core needs (clock, filesystem, HTTP) are passed in as callables or small protocol objects, so tests substitute them without patching.
