# AI Tool Calling via Existing Service Interfaces

**Category:** ai
**Status:** Active
**Requires:** `adrs/dotnet/modular-monolith.md`, `adrs/dotnet/service-layer-logic.md`, `adrs/ai/meai-abstraction.md`
**Conflicts with:** `adrs/ai/tool-calling-via-services-python.md`

## Decision
AI tools (functions the LLM can invoke) are thin adapters in the AI module that delegate to existing module service interfaces. Tools contain no business logic — they parse parameters, call a service method, and return the result. Tool definitions are created with `AIFunctionFactory.Create()` or `AIFunction` from M.E.AI.

## Rationale
- Reusing existing service interfaces as the implementation behind AI tools avoids duplicating business logic. The same `ICatalogService.SearchProductsAsync()` that powers the REST API also powers the AI tool, ensuring consistent behavior and a single source of truth.
- Alternatives considered: letting the LLM generate raw SQL or LINQ queries (rejected — dangerous and uncontrollable), building separate "AI-only" services (rejected — duplicates logic and creates drift between API and AI behavior), using Semantic Kernel plugins (rejected as primary approach — ties tool definitions to SK's abstraction rather than M.E.AI's).
- Tools live in the AI module's `Tools/` folder, making them easy to audit and manage. Each tool's `[Description]` attribute provides the LLM with clear usage instructions.
- Tool results are DTOs or primitives, ensuring they serialize cleanly for the LLM's consumption.

## Constraints (non-negotiable for AI)
- AI tools MUST be thin adapters only: parse parameters, call a service interface method, return the result.
- AI tools MUST delegate to shared contract interfaces. NEVER access another module's internals (DbContext, entities, repositories) directly.
- Tool definitions MUST live in the AI module's `Tools/` folder.
- Every tool method MUST have a `[Description]` attribute that clearly describes the tool's purpose for LLM consumption.
- Tool results MUST be serializable: DTOs or primitive types only. NEVER return EF entities or complex internal objects.
- NEVER give a tool unrestricted database query capability (e.g., raw SQL execution or open-ended LINQ).
- NEVER allow tools to perform destructive operations (deletes, hard mutations) without an explicit confirmation mechanism (e.g., human approval or a two-step confirm parameter).
- Tool classes MUST receive their dependencies (service interfaces) via constructor injection.
