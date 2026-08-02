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
| `family` | no | Kebab-case slug linking sibling ADRs that answer the same architectural question with per-stack or per-tool variants (e.g., `structured-logging` links the .NET and Python logging ADRs). Members must be exclusive per system — different concrete stacks or mutual `conflicts_with`. The validator enforces both, plus a review-drift warning: when one sibling is re-reviewed, re-verify the family together. |
| `verify_against` | no | List of frameworks/packages (with major version where relevant) whose releases can invalidate this ADR's claims — e.g., `Tailwind CSS 4`, `TanStack Query 5`. Marks the ADR as version-sensitive: `python scripts/validate_adrs.py --stale [months]` lists version-sensitive ADRs whose `last_reviewed` has aged past the threshold (default 6 months). |

Paths are repo-relative (`adrs/<category>/<file>.md`), plain — no backticks or quotes. The frontmatter is a strict subset of YAML enforced by `scripts/validate_adrs.py`.

## Examples Section (optional)

For constraints that agents commonly violate, add an `## Examples` section after the Constraints with **violation → compliant** pairs:

```markdown
## Examples

**Violation — blocking on async:**
```csharp
var product = _service.GetProductByIdAsync(id).Result;
```

**Compliant:**
```csharp
var product = await _service.GetProductByIdAsync(id, ct);
```
```

Rules of thumb: one pair per commonly violated constraint (not per constraint), keep each snippet under ~10 lines, and make the violation the realistic mistake — not a strawman. Examples are normative illustrations of existing constraints; they never introduce new rules.

## Lifecycle

ADRs are never deleted — their history is part of the record:

- **Active** — the decision is current and enforced.
- **Deprecated** — the decision no longer applies and has no direct replacement. Keep the file; new projects must not select it.
- **Superseded** — a newer ADR replaces this one. Set `status: Superseded` and add `superseded_by: adrs/<path>.md` pointing at the replacement (the validator enforces that the two always appear together).

Update `last_reviewed` whenever an ADR is re-verified against current framework versions — stale review dates signal where version-sensitive claims may have drifted.
