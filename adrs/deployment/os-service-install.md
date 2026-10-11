---
category: deployment
stack: any
status: Active
requires: []
conflicts_with: []
last_reviewed: 2026-10-11
---

# Installed as an OS Service

## Decision
A long-running agent installs itself as a service of the host operating system (a Windows Task Scheduler task, a systemd user unit, a launchd agent) through one idempotent command of its own (`<tool> service install|uninstall|status`), runs as the operator's user, restarts on failure, logs to the platform's facility or a rotated file, and never requires a container or a supervisor the host does not already have.

## Rationale
- An agent that only runs while a terminal is open is not an agent. The host already has a service manager, and using it gives start-on-boot, restart and log retention without adding a runtime.
- The install command is part of the tool so that the same code path works on every supported platform and is tested; copying unit files by hand is the step people get wrong.
- Alternatives considered: Docker on the workstation (rejected; a container cannot see the host processes and files the agent exists to watch, and it adds a runtime to every developer machine), NSSM or pm2 (rejected; a second tool to install), a tray application (viable later as an adapter; not the service mechanism).

## Constraints (non-negotiable for AI)
- `service install` is idempotent and reversible; `uninstall` removes exactly what `install` created. Both print what they did and exit non-zero with a plain message when they lack permission.
- The service runs as the operator's user, NEVER as root or SYSTEM: it needs the operator's files and nothing more.
- Restart on failure with a backoff the platform supports. The service MUST NOT restart in a tight loop on a configuration error: detect, log, exit with a distinct code the manager does not restart.
- Logs go to the platform facility (journald, Event Log, unified logging) or to a rotated file in the state directory. NEVER unbounded files.
- Configuration (hub URL, token source, state directory) is read from the environment of the service definition, written by `install` from the operator's answers. The token is referenced, NEVER embedded in the unit file in plain text where the platform offers a secret store.
