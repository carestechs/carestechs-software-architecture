---
category: deployment
stack: dotnet
status: Active
requires: []
conflicts_with: []
last_reviewed: 2026-07-29
---

# Maintenance CLI and Scheduler Workers

## Decision

Recurring maintenance tasks (data migration, pipeline verification, metric recomputation) are implemented as named routines in a dual-mode worker: a CLI for on-demand execution (`run <routine> [--dry-run]`) and a scheduler for periodic automated execution. Routines are registered in DI and selected by name at runtime.

## Rationale

- Operations teams need to run maintenance routines ad-hoc (investigate, backfill, verify) without deploying or scheduling — a CLI mode with `list` and `run <name>` commands enables this with familiar tooling
- Recurring tasks (nightly imports, hourly validations) need a scheduler mode that runs the same routines on a configurable interval without requiring external cron infrastructure
- A `--dry-run` flag on every routine lets operators preview side effects before committing, which is critical for data migration and cleanup tasks that touch production state
- Registering routines by name in DI keeps the worker extensible — new routines are added by implementing an interface and registering in `Program.cs`, with no changes to the CLI framework

## Constraints (non-negotiable for AI)

- The worker `Program.cs` MUST support two modes based on command-line arguments: (1) CLI mode — `list` prints all registered routine names; `run <routine-name>` executes a single routine by name and exits. (2) Scheduler mode — default (no arguments) starts a `BackgroundService` that runs configured routines on a timer interval
- Every routine MUST support a `--dry-run` flag that logs what would happen without modifying any state — this applies to both CLI and scheduler modes
- Routines MUST be registered in DI as implementations of a common interface (e.g., `IMaintenanceRoutine` with `Name`, `ExecuteAsync(options)`) and resolved by name at runtime — NEVER use `if/switch` on routine name strings in `Program.cs`
- The CLI `run` command MUST accept optional flags (e.g., `--no-aws` for offline testing) and pass them to the routine via an options object — NEVER use environment variables for per-invocation flags
- Routines that write to external systems (databases, APIs, queues) MUST log the action and target before executing, so the operator can verify correctness from the output
- The scheduler `BackgroundService` MUST read its interval and enabled routine list from configuration (the parameters provider of `adrs/deployment/aws-secrets-parameters.md` in AWS deployments, or the stack's configuration ADR otherwise) — NEVER hardcode schedule intervals
