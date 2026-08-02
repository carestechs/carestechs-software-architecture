---
category: ai
stack: any
status: Active
requires:
  - adrs/dotnet/modular-monolith.md | adrs/python/modular-packages.md
  - adrs/dotnet/service-layer-logic.md | adrs/python/service-layer-logic.md
  - adrs/ai/llm-abstraction.md
conflicts_with: []
last_reviewed: 2026-08-01
---

# AI Tool Calling via Existing Service Interfaces

## Decision
AI tools (functions the LLM can invoke) are thin adapters in the AI module that delegate to existing module service interfaces. Tools contain no business logic — they parse parameters, call a service method through the shared contract interface, and return the result.

## Rationale
- Reusing existing service interfaces as the implementation behind AI tools avoids duplicating business logic: the same service that powers the REST API powers the AI tool, ensuring consistent behavior and a single source of truth.
- Alternatives considered: letting the LLM generate raw SQL or query expressions (rejected — dangerous and uncontrollable), building separate "AI-only" services (rejected — duplicates logic and creates drift between API and AI behavior), auto-generating tools from API endpoints (rejected — couples tool definitions to the HTTP layer unnecessarily).
- Tools live in the AI module's tools area, making them easy to audit; each tool's description is written for LLM comprehension.
- Tool results are DTOs or primitives, ensuring they serialize cleanly for the LLM's consumption.

## Constraints (non-negotiable for AI)
- AI tools MUST be thin adapters only: parse parameters, call a service interface method, return the result. NEVER place business logic in a tool.
- Tools MUST delegate through shared contract interfaces. NEVER access another module's internals (data context, entities, repositories, service implementations) directly.
- Every tool MUST carry a clear name and description written for LLM comprehension, plus an explicit parameter schema.
- Tool results MUST be serializable DTOs or primitives. NEVER return ORM entities or complex internal objects.
- NEVER give a tool unrestricted database query capability (raw SQL execution or open-ended query builders/LINQ).
- NEVER allow tools to perform destructive operations (deletes, hard mutations) without an explicit confirmation mechanism (e.g., human approval or a two-step confirm parameter).

**.NET mechanics:**
- Tools live in the AI module's `Tools/` folder as classes receiving service interfaces via constructor injection.
- Definitions are created with `AIFunctionFactory.Create()` / `AIFunction` from M.E.AI; every tool method carries a `[Description]` attribute.

**Python mechanics:**
- Tools live in the AI module's `tools/` sub-package, one tool per file, as `async def` functions when they call async services.
- Results MUST be Pydantic-serializable; service access goes through the contracts package.

## Examples

**Violation — business logic and direct data access inside a tool:**
```csharp
[Description("Searches products")]
public async Task<string> SearchProducts(string term)
{
    var results = await _db.Products // another module's data, queried directly
        .Where(p => p.Name.Contains(term)).ToListAsync();
    return JsonSerializer.Serialize(results);
}
```

**Compliant (.NET):**
```csharp
[Description("Searches the product catalog by name or SKU")]
public async Task<IReadOnlyList<ProductDto>> SearchProducts(string term, CancellationToken ct)
    => await _catalogService.SearchProductsAsync(term, ct); // same service the REST API uses
```

**Compliant (Python):**
```python
async def search_products(term: str) -> list[ProductRead]:
    """Search the product catalog by name or SKU."""
    return await catalog_service.search_products(term)  # same service the API uses
```
