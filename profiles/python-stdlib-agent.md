# Stack Profile: Python Standard-Library Agent

**Status:** Active
**Assumes:** Python 3.11+ as the interpreter floor on the host, no third-party packages, Windows 10+ / Linux with systemd / macOS; a hub it reports to over HTTPS (any stack)

## Overview

A curated set of ADRs for a small program that runs unattended on machines the author does not control, watches something local (a log, a process, a directory) and talks to a central hub: a sidecar, an agent, a collector. It has no web framework, no database and no dependencies; it ships as one file or a zipapp, dials out and never listens, keeps its few hundred bytes of state in files written atomically, and installs itself as a service of the host operating system.

The core/adapter shape is what keeps such a program testable: the logic is plain functions and dataclasses with typed entry points, and everything that touches the world (command line, daemon loop, loopback hook, file tail, service manager, HTTP to the hub) is an adapter a handful of tests can cover.

What this profile deliberately does NOT have: a package manager on the target, inbound ports, a container, a database, a third-party HTTP client or test runner. If the program grows a user interface or a data model, it has become a product and belongs in a product profile; this one stays a tool.

---

## Solution Structure

```
my-agent/
├── agent.py                        # single-file entry: parses argv, dispatches to adapters
├── build_zipapp.py                 # stdlib script producing dist/my-agent.pyz
├── .env.example                    # HUB_URL, HUB_TOKEN (source), STATE_DIR
│
├── core/
│   ├── types.py                    # dataclasses shared by core and adapters
│   ├── classify.py                 # pure logic: events -> progress / attention / terminal
│   ├── commands.py                 # the fixed command vocabulary and its validation
│   └── engine.py                   # one typed entry point per operation
│
├── adapters/
│   ├── cli.py                      # argparse; exit codes
│   ├── daemon.py                   # the loop: tail, post, long-poll, execute, ack
│   ├── hub_client.py               # urllib over HTTPS, backoff, token from env
│   ├── tail.py                     # file tail with cursor files
│   ├── hook.py                     # http.server on 127.0.0.1 for local producers
│   ├── state.py                    # atomic file state
│   └── service.py                  # install | uninstall | status per platform
│
└── tests/                          # unittest; core tested without any process or socket
```

---

## Required (core to this stack — do not omit)

These ADRs define the fundamental architecture. Removing any of them breaks the coherence of the stack.

<!-- generated from profiles/profiles.toml — edit the manifest and run scripts/generate_profiles.py -->
| ADR | Summary | Depends On |
|-----|---------|-------------|
| `adrs/python/stdlib-only-distribution.md` | Standard library only, at runtime and in tests; ships as one file or a zipapp; `unittest` as the runner; interpreter floor checked at start. | — |
| `adrs/python/core-adapter-pattern.md` | Core engine of plain functions and dataclasses with one typed entry point per operation; thin adapters (CLI, daemon loop, loopback hook, file tail, service) own all I/O. The core never imports an adapter. | — |
| `adrs/deployment/outbound-only-agent.md` | The agent dials out for everything (POST events, long-poll commands over HTTPS with a token from the environment), binds nothing but loopback, pulls a fixed command vocabulary and acknowledges by id. | — |
| `adrs/deployment/file-state-atomic-writes.md` | Small local state (cursors, acknowledgements, identity) in JSON files in one directory, written temp-then-rename, tolerated missing or corrupt at startup. Secrets are never state files. | — |
| `adrs/deployment/env-connection-urls.md` | All config via environment variables read by the service adapter at start: hub URL, token source, state directory. No config file in the repository; `.env.example` documents the names. | — |

## Recommended (strong defaults — can be swapped with noted alternatives)

These are battle-tested defaults. You can swap them, but you should have a good reason.

<!-- generated from profiles/profiles.toml — edit the manifest and run scripts/generate_profiles.py -->
| ADR | Summary | Alternative |
|-----|---------|-------------|
| `adrs/deployment/os-service-install.md` | One idempotent `service install|uninstall|status` command registers the agent with the host's service manager (Task Scheduler, systemd user unit, launchd), running as the operator with restart on failure and platform logging. | A supervisor the host already has (Docker, NSSM, pm2) - never a new one |
| `adrs/deployment/github-actions-ci.md` | CI gate on every push/PR: `python -m unittest discover`, the zipapp build, and a smoke run of the artifact on each supported OS. | GitLab CI / Azure DevOps (if repos move) |

## Optional (pick based on project needs)

<!-- generated from profiles/profiles.toml — edit the manifest and run scripts/generate_profiles.py -->
| ADR | Summary | When to Include |
|-----|---------|-------------|
| `adrs/deployment/private-network-exposure.md` | Reachable only on a private overlay network (tailnet or VPN): server bound to the overlay, TLS from it, application tokens unchanged as the second lock. No public name, no open port. | Personal or team tools whose users can all join the network; swap for a public endpoint with TLS when they cannot |

---

## Key Cross-Cutting Concerns

- **Direction of trust:** the agent initiates every connection, authenticates with a token from its environment, pulls a fixed vocabulary of commands and acknowledges by id. The hub never reaches in; the only listener is loopback.
- **Offline is normal:** events buffer to disk while the hub is unreachable and drain on reconnect; backoff is bounded and jittered. Nothing is lost because the network was down, and nothing is executed twice because the agent acknowledged late (handlers are idempotent per command id).
- **State is recoverable, secrets are not state:** cursors and acknowledgements live in atomically written files under one directory; deleting the directory costs a resync. Tokens come from the environment or the OS secret store.
- **Install is a command:** `service install` registers the agent with the host's service manager as the operator's user, with restart on failure and platform logging; `uninstall` reverses exactly that.
- **Testing without the world:** the core is exercised with dataclasses in and out; adapters are tested at their seams (a fake hub over `http.server` on loopback, a temporary state directory, a fake clock). `python -m unittest discover` is the whole test command.
- **One file to ship:** the zipapp is the artifact; the interpreter floor is checked at start with a plain message.

## Development Workflow

### Local Development Commands

```bash
# run from source
python agent.py --help
HUB_URL=https://hub.<overlay-domain> HUB_TOKEN=... python agent.py run --once

# tests (no server, no network)
python -m unittest discover -s tests

# build the artifact
python build_zipapp.py        # -> dist/my-agent.pyz
python dist/my-agent.pyz --version
```

### Installing on a host

```bash
# copy dist/my-agent.pyz to the host, then:
python my-agent.pyz service install      # asks for hub URL and token source, registers the service
python my-agent.pyz service status
python my-agent.pyz service uninstall
```
