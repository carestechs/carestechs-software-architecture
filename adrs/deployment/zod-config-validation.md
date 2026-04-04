# Zod for Runtime Validation at System Boundaries

**Category:** deployment
**Status:** Active
**Requires:** `adrs/typescript/strict-typescript.md`

## Decision

Use Zod for runtime validation of all external input that crosses system boundaries — configuration files, environment variables, AI/LLM responses, and API responses from external services. TypeScript types are derived from Zod schemas where possible to keep validation and types in sync.

## Rationale

- TypeScript's type system is erased at runtime. Config files, environment variables, and AI responses arrive as untyped data. Without runtime validation, a malformed config produces cryptic errors deep in the call stack instead of a clear message at the boundary.
- Zod provides a schema-first approach where the TypeScript type is inferred from the schema (`z.infer<typeof schema>`), eliminating the risk of the type and validation logic drifting apart.
- Alternatives considered: manual validation with type guards (rejected — verbose, error-prone, no schema reuse), Joi (rejected — weaker TypeScript integration, no type inference), io-ts (rejected — steeper learning curve, functional style less familiar to most teams).

## Constraints (non-negotiable for AI)

- All config file parsing MUST validate through a Zod schema before use.
- All environment variable reading MUST validate through a Zod schema at startup.
- All AI/LLM responses that are expected to have structure MUST be validated through a Zod schema before being treated as typed data.
- TypeScript types for validated data SHOULD be derived from Zod schemas using `z.infer<>` — NEVER define the type separately and hope it matches.
- Validation errors MUST produce clear, user-facing messages that identify the field and the problem.
- NEVER validate inside core business logic — validation happens once, at the ingestion boundary. Internal function-to-function calls trust the types.
