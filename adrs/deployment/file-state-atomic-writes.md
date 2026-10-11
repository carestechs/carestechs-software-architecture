---
category: deployment
stack: any
status: Active
requires: []
conflicts_with: []
last_reviewed: 2026-10-11
---

# Local State in Files with Atomic Writes

## Decision
A single-process agent or daemon keeps its small local state (cursors into the logs it tails, pending acknowledgements, the last-seen run id, its own identity) in plain files in one directory, written by write-to-temporary-then-rename, read at startup, and treated as recoverable: losing the directory costs a resync, never correctness.

## Rationale
- A sidecar's state is a few hundred bytes that must survive its own restart and nothing else. A database for it is a dependency and an operations surface for no gain; files are inspectable with `cat` and recoverable with `rm`.
- Atomic replace (`os.replace` of a fully written temporary file in the same directory) is the one discipline that makes files safe: a crash mid-write leaves the old state, never a half-written one.
- Alternatives considered: SQLite (viable when the state has structure or several writers; overkill for cursors), a registry or OS keychain (reserved for secrets, not state), memory only (rejected; every restart would replay or lose work).

## Constraints (non-negotiable for AI)
- One state directory, configurable, under the user's data directory by default. Every file in it is JSON or plain text with a schema version field.
- Every write is: serialize fully, write `<name>.tmp` in the same directory, flush and fsync, replace over the target. NEVER write into the target in place.
- Startup tolerates a missing or corrupt file: log it, start from the empty state and resync from the source. NEVER crash on it.
- Secrets (tokens, keys) are NEVER state files; they come from the environment or the OS secret store.
- A cursor file records the identity of what it indexes (path plus inode or size of the log being tailed), so a rotated or replaced source is detected and the cursor reset instead of misread.

## Examples

**Violation — in-place write:**
```python
with open(state_dir / "cursor.json", "w") as f:
    json.dump(state, f)           # a crash here leaves a truncated file
```

**Compliant:**
```python
tmp = state_dir / "cursor.json.tmp"
with open(tmp, "w", encoding="utf-8") as f:
    json.dump(state, f); f.flush(); os.fsync(f.fileno())
os.replace(tmp, state_dir / "cursor.json")
```
