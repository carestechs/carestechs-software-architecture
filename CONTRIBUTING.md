# Contributing to Architecture Decision Records

## How to Add a New ADR

### 1. Choose the Right Category

| Category | For | Folder |
|----------|-----|--------|
| dotnet | .NET / C# architecture and conventions | `adrs/dotnet/` |
| python | Python / FastAPI architecture and conventions | `adrs/python/` |
| typescript | TypeScript / Node.js architecture and conventions | `adrs/typescript/` |
| angular | Angular frontend patterns | `adrs/angular/` |
| react | React frontend patterns | `adrs/react/` |
| database | Database design, naming, types | `adrs/database/` |
| api | REST API design, auth, pagination | `adrs/api/` |
| ai | AI agent modules, LLM abstraction, RAG, tool calling | `adrs/ai/` |
| deployment | Docker, configuration, infrastructure, dev/prod topology | `adrs/deployment/` |

### 2. Use the ADR Format

Create a file with kebab-case naming in the appropriate category folder. See `ADR-FORMAT.md` for the full template:

```markdown
---
category: [folder name]
stack: dotnet | python | typescript | angular | react | any
status: Active
requires: []          # ADR dependencies; " | " inside an item separates alternatives
conflicts_with: []    # mutually exclusive ADRs — always declared symmetrically
last_reviewed: YYYY-MM-DD
---

# [Decision Title]

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
- [ ] Frontmatter includes `category`, `stack`, `status`, `requires`, `conflicts_with` (use `[]` when empty)
- [ ] `stack` names the technology stack the constraints assume (`any` for cross-stack); in language category folders it equals the folder name
- [ ] `requires` and `conflicts_with` list actual repo-relative ADR paths, one requirement per list item (in `requires`, separate alternatives with ` | ` inside a single item when any one of them satisfies the dependency)
- [ ] Conflicts are **symmetric**: if this ADR lists another in `conflicts_with`, edit that ADR to list this one back (language-specific variants must conflict with each other in both directions)
- [ ] `python scripts/validate_adrs.py` passes with no new errors
- [ ] Constraints that agents commonly get wrong have a violation → compliant pair in an `## Examples` section (see ADR-FORMAT.md)

## How to Create a New Profile

Profiles are curated sets of ADRs for specific tech stack and deployment mode combinations.

### 1. Define the Stack

```markdown
# Stack Profile: [Stack Name] ([Deploy Mode])

**Status:** Active
**Assumes:** [Runtime versions, database, deployment tooling, key dependencies]
```

### 2. List ADRs by Tier

Organize ADRs into three tiers. Include both application architecture and deployment ADRs together:

- **Required** — Core to the stack's coherence. Removing any breaks the architecture.
- **Recommended** — Battle-tested defaults. Can be swapped with noted alternatives.
- **Optional** — Project-specific concerns. Include based on needs.

### 3. Add Cross-Cutting Concerns

Document patterns that emerge from the combination of ADRs (naming translation, auth flow, ID strategy, deployment topology, etc.).

### 4. Add Solution Structure

Include a representative file tree showing how the stack's conventions map to project structure, including deployment files (Dockerfiles, Compose files, config templates).

### 5. Add Development Workflow

Include concrete commands for local development setup and production deployment.

### Naming Convention

Profile names encode both the stack and the deployment mode:

```
[backend]-[frontend]-[architecture]-[deploy-mode].md
```

Examples:
- `python-react-modular-monolith-docker-compose.md`
- `dotnet-angular-ai-agent-docker-compose.md`

## Naming Conventions

- **File names**: `kebab-case.md` (e.g., `modular-monolith.md`, `uuid-primary-keys.md`)
- **Category folders**: Match the Category field values exactly
- **Profile names**: `[backend]-[frontend]-[architecture]-[deploy-mode].md`
- **Language variants**: Use `-python` suffix for Python variants when a .NET version exists

## Lifecycle

Never delete an ADR. When a decision changes:

- Set `status: Deprecated` when the decision no longer applies and has no replacement.
- Set `status: Superseded` and add `superseded_by: adrs/<path>.md` when a newer ADR replaces it (the validator enforces this pairing).
- Update `last_reviewed` (YYYY-MM-DD) whenever an ADR is re-verified against current framework versions.

## Review Process

1. Run `python scripts/validate_adrs.py` (CI runs it on every push/PR) — it checks frontmatter format, that referenced files exist, conflict symmetry, requires cycles, and profile consistency (selected sets must not conflict, and their requires must be satisfiable)
2. Check constraint phrasing — rules should be concrete and testable
3. Verify the ADR doesn't duplicate an existing ADR's scope
4. If creating a language variant, ensure both variants declare `conflicts_with` against each other (the validator enforces symmetry)
5. Run a test compilation using `compile-adrs.md` to verify the ADR integrates correctly
6. After merging, tag a release when the change warrants it (see the Versioning section of README.md): new ADRs or constraints bump MINOR, clarifications bump PATCH, format/semantic breaks bump MAJOR
