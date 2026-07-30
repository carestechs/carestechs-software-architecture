# ADR Format Template

> Copy this template when creating a new Architecture Decision Record. Fill in every section.

---

# [Decision Title]

**Category:** dotnet | python | typescript | angular | react | database | api | ai | deployment
**Stack:** dotnet | python | typescript | angular | react | any — [the technology stack this ADR's constraints assume; `any` means cross-stack. In a language category folder (dotnet/python/typescript/angular/react) this must equal the folder name. Same-slot variants for different stacks (e.g., the .NET and Python RAG ADRs) declare mutual Conflicts; the Stack field tells a consumer which variant applies to their project.]
**Status:** Active
**Requires:** [comma-separated ADR file paths this decision depends on, e.g., `adrs/dotnet/modular-monolith.md` — use — if none. When any one of several ADRs satisfies a dependency, separate the alternatives with ` | ` (e.g., `adrs/dotnet/modular-monolith.md` | `adrs/dotnet/clean-architecture-layers.md`)]
**Conflicts with:** [comma-separated ADR file paths that are mutually exclusive with this decision — use — if none. Conflicts MUST be declared symmetrically: if this ADR lists another, that ADR must list this one back]
**Last reviewed:** [YYYY-MM-DD — optional; the date the ADR was last verified against current framework/library versions]

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

## Lifecycle

ADRs are never deleted — their history is part of the record:

- **Active** — the decision is current and enforced.
- **Deprecated** — the decision no longer applies and has no direct replacement. Keep the file; new projects must not select it.
- **Superseded** — a newer ADR replaces this one. Set `**Status:** Superseded` and add `**Superseded by:** `adrs/<path>.md`` pointing at the replacement (the validator enforces that the two always appear together).

Update `**Last reviewed:**` whenever an ADR is re-verified against current framework versions — stale review dates signal where version-sensitive claims may have drifted.
