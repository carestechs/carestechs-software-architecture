# ADR Format Template

> Copy this template when creating a new Architecture Decision Record. Fill in every section.

---

---
category: <folder name>
stack: dotnet | python | typescript | angular | react | any
status: Active
requires: []
conflicts_with: []
last_reviewed: YYYY-MM-DD
---

# [Decision Title]

## Decision

[1-2 sentences: what was decided]

## Rationale

- [Why this decision was made]
- [What alternatives were considered]

## Constraints (non-negotiable for AI)

- [Hard rule 1 — the AI must never violate this]
- [Hard rule 2]
- [Hard rule 3]

---

## Field Reference

| Key | Required | Meaning |
|-----|----------|---------|
| `category` | yes | Must match the folder the file lives in. |
| `stack` | yes | The technology stack the constraints assume; `any` = cross-stack. In language folders (dotnet/python/typescript/angular/react) it must equal the folder name. Same-slot variants for different stacks declare mutual `conflicts_with`; `stack` tells a consumer which variant applies. |
| `status` | yes | `Active`, `Deprecated`, or `Superseded`. |
| `requires` | yes | List of ADR paths this decision depends on (`[]` when none). Each list item is one requirement; separate alternatives inside an item with ` \| ` when any one of them satisfies it (e.g., `adrs/dotnet/modular-monolith.md \| adrs/dotnet/clean-architecture-layers.md`). |
| `conflicts_with` | yes | Flat list of mutually exclusive ADR paths (`[]` when none). MUST be symmetric: if this ADR lists another, that ADR must list this one back. No alternatives. |
| `last_reviewed` | no | `YYYY-MM-DD` — when the ADR was last verified against current framework/library versions. |
| `superseded_by` | no | Path of the replacing ADR. Present if and only if `status: Superseded`. |

Paths are repo-relative (`adrs/<category>/<file>.md`), plain — no backticks or quotes. The frontmatter is a strict subset of YAML enforced by `scripts/validate_adrs.py`.

## Lifecycle

ADRs are never deleted — their history is part of the record:

- **Active** — the decision is current and enforced.
- **Deprecated** — the decision no longer applies and has no direct replacement. Keep the file; new projects must not select it.
- **Superseded** — a newer ADR replaces this one. Set `status: Superseded` and add `superseded_by: adrs/<path>.md` pointing at the replacement (the validator enforces that the two always appear together).

Update `last_reviewed` whenever an ADR is re-verified against current framework versions — stale review dates signal where version-sensitive claims may have drifted.
