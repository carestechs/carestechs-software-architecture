---
category: python
stack: python
status: Active
requires: []
conflicts_with:
  - adrs/python/fastapi-framework.md
  - adrs/python/pytest-testing.md
last_reviewed: 2026-10-11
---

# Standard-Library-Only Distribution

## Decision
The program depends on the Python standard library only: no third-party package at runtime or in its tests. It ships as one file or a zipapp that runs with `python <file>` on any supported interpreter, with `unittest` as its test runner, and states its interpreter floor in one place, checked at start.

## Rationale
- The program runs on machines its author does not control (developer workstations, build hosts, a colleague's laptop), where installing packages is a step people skip, a virtual environment is one more thing to break, and a dependency's own update can take the tool down. Zero dependencies is the only install story that survives that.
- The surface such a program needs (HTTP client and server, JSON, subprocess, sqlite3, logging, argparse, threading, asyncio) is in the standard library. What is lost (a more ergonomic HTTP client, a nicer test runner) is small against never having an install failure.
- Alternatives considered: a vendored wheel set (rejected; a dependency tree with extra steps), PyInstaller binaries (viable for end users without Python; an adapter over the same zipapp, not a replacement, and a build-matrix cost), pytest "because it is nicer" (rejected; it would be the first dependency, and the test suite would be what stops a dependency-free tool from being dependency-free).

## Constraints (non-negotiable for AI)
- NEVER import a package outside the standard library in the program or its tests. `requirements.txt`, `pyproject.toml` dependencies and `pip install` steps MUST NOT exist for it.
- The supported interpreter floor is declared once (a constant) and checked at startup with a plain message. NEVER use a feature above the floor.
- Tests run with `python -m unittest discover` and nothing else: no plugins, no fixture framework.
- Distribution is one file or one `.pyz` built by `zipapp` with a standard-library build script. NEVER require a package manager on the target machine.
- A third-party dependency the program cannot do without is a graduation recorded as an ADR change, not an exception.
