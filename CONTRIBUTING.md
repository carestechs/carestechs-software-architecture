# Contributing to Architecture Decision Records

## How to Add a New ADR

### 1. Choose the Right Category

| Category | For | Folder |
|----------|-----|--------|
| dotnet | .NET / C# architecture and conventions | `adrs/dotnet/` |
| python | Python / FastAPI architecture and conventions | `adrs/python/` |
| angular | Angular frontend patterns | `adrs/angular/` |
| react | React frontend patterns | `adrs/react/` |
| database | Database design, naming, types | `adrs/database/` |
| api | REST API design, auth, pagination | `adrs/api/` |
| ai | AI agent modules, LLM abstraction, RAG, tool calling | `adrs/ai/` |

### 2. Use the ADR Format

Create a file with kebab-case naming in the appropriate category folder. See `ADR-FORMAT.md` for the full template:

```markdown
# [Decision Title]

**Category:** [category]
**Status:** Active
**Requires:** [file paths of ADR dependencies, or omit if none]
**Conflicts with:** [file paths of mutually exclusive ADRs, or omit if none]

## Decision
[1-2 sentences: what was decided]

## Rationale
- [Why — be specific about the problem this solves]
- [Why — alternatives considered or industry best practice cited]

## Constraints (non-negotiable for AI)
- [Hard rule 1 — the AI must never violate this]
- [Hard rule 2 — concrete, testable, actionable]
```

### 3. Quality Checklist

Before submitting:

- [ ] File uses kebab-case naming (e.g., `dbcontext-per-module.md`)
- [ ] Category field matches the folder name exactly
- [ ] Decision section is 1-2 sentences, clear and actionable
- [ ] Rationale has at least 2 bullets explaining *why*
- [ ] At least 2 Constraints are defined
- [ ] Constraints use concrete, testable language (MUST/NEVER phrasing)
- [ ] `Requires` field lists actual ADR file paths (or is omitted)
- [ ] `Conflicts with` is checked — no two selected ADRs should conflict
- [ ] Language-specific variants declare `Conflicts with` against the other variant

## How to Create a New Profile

Profiles are curated sets of ADRs organized into Required, Recommended, and Optional tiers for specific tech stack combinations.

### 1. Define the Stack

```markdown
# Stack Profile: [Stack Name]

**Status:** Active
**Assumes:** [Runtime versions, database, key dependencies]
```

### 2. List ADRs by Tier

Organize ADRs into three tiers:

- **Required** — Core to the stack's coherence. Removing any breaks the architecture.
- **Recommended** — Battle-tested defaults. Can be swapped with noted alternatives.
- **Optional** — Project-specific concerns. Include based on needs.

### 3. Add Cross-Cutting Concerns

Document patterns that emerge from the combination of ADRs (naming translation, auth flow, ID strategy, etc.).

### 4. Add Solution Structure

Include a representative file tree showing how the stack's conventions map to project structure.

## Naming Conventions

- **File names**: `kebab-case.md` (e.g., `modular-monolith.md`, `uuid-primary-keys.md`)
- **Category folders**: Match the Category field values exactly
- **Profile names**: Stack description in kebab-case (e.g., `dotnet-angular-modular-monolith.md`)
- **Language variants**: Use `-python` suffix for Python variants when a .NET version exists

## Review Process

1. Verify `Requires` and `Conflicts with` fields reference valid ADR file paths
2. Check constraint phrasing — rules should be concrete and testable
3. Verify the ADR doesn't duplicate an existing ADR's scope
4. If creating a language variant, ensure it declares `Conflicts with` against the other variant
5. Run a test compilation using `compile-adrs.md` to verify the ADR integrates correctly
