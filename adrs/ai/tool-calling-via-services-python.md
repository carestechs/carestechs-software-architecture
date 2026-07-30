---
category: ai
stack: python
status: Active
requires:
  - adrs/python/modular-packages.md
  - adrs/python/service-layer-logic.md
  - adrs/ai/llm-abstraction-python.md
conflicts_with:
  - adrs/ai/tool-calling-via-services.md
last_reviewed: 2026-07-29
---

# Tool Calling Delegates to Service Layer

## Decision
AI tools (functions the LLM can call) are thin adapters that delegate to existing service functions. Tools live in the AI module's `tools/` sub-package. Each tool parses LLM-provided parameters, calls a service function from the appropriate module via its contract interface, and returns the result. No business logic lives in tool definitions.

## Rationale
- Making tools thin adapters ensures that the same business logic is used whether triggered by an API endpoint or by an LLM tool call. This prevents logic duplication and ensures consistent behavior.
- Alternatives considered: business logic in tool functions (rejected — duplicates service logic, makes testing harder, creates two paths for the same operation), auto-generating tools from API endpoints (rejected — couples tool definitions to HTTP layer unnecessarily).
- Tools import service interfaces from the shared contracts package, maintaining module boundaries.
- Tool descriptions and parameter schemas are critical for LLM understanding — they must be clear and well-documented.

## Constraints (non-negotiable for AI)
- AI tools MUST live in the AI module's `tools/` sub-package, one tool per file.
- Tools MUST be thin adapters: parse parameters, call a service function, return the result. NEVER place business logic in a tool.
- Tools MUST delegate to service functions via shared contract interfaces. NEVER import service implementations directly from other modules.
- Every tool MUST have a clear `name`, `description`, and parameter schema. Descriptions MUST be written for LLM comprehension.
- Tool functions MUST be `async def` when they call async services.
- Tools MUST return Pydantic-serializable results. NEVER return raw ORM model instances from tools.
- NEVER allow tools to perform destructive operations (DELETE, hard mutations) without an explicit confirmation mechanism (e.g., human approval or a two-step confirm parameter).
- NEVER give a tool unrestricted database query capability (e.g., raw SQL execution or open-ended query builders).

## Examples

**Violation — direct data access and logic inside a tool:**
```python
async def search_products(term: str) -> list[dict]:
    async with async_session() as session:      # tool talks to the database itself
        rows = await session.execute(
            select(Product).where(Product.name.contains(term)))
        return [row._asdict() for row in rows]
```

**Compliant:**
```python
async def search_products(term: str) -> list[ProductRead]:
    """Search the product catalog by name or SKU."""
    return await catalog_service.search_products(term)  # same service the API uses
```
