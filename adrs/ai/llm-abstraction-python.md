---
category: ai
stack: python
status: Active
requires:
  - adrs/python/async-all-the-way.md
  - adrs/python/service-layer-logic.md
conflicts_with:
  - adrs/ai/meai-abstraction.md
  - adrs/ai/claude-agent-sdk.md
last_reviewed: 2026-07-29
---

# Provider-Agnostic LLM Abstraction Layer

## Decision
All LLM and embedding calls go through a provider-agnostic abstraction layer. Service code depends on abstract interfaces (protocols or ABC classes) for chat completion and embedding generation. Provider-specific SDKs (Anthropic, OpenAI, etc.) are only imported in the composition root or adapter modules. The default abstraction is a thin custom adapter (a Protocol plus one adapter module per provider); adopt LangChain as the abstraction only when its ecosystem (chains, loaders, integrations) is concretely needed. Either way, service code never imports provider SDKs directly.

## Rationale
- A provider-agnostic layer allows swapping LLM providers (OpenAI, Anthropic, Azure OpenAI, local models) without changing service code. This is critical for an AI-first application where provider capabilities, pricing, and availability evolve rapidly.
- Alternatives considered: direct Anthropic/OpenAI SDK usage throughout (rejected — creates hard provider coupling, makes switching expensive), LangChain as the sole abstraction (rejected as the default — heavier dependency and its own orchestration model; acceptable when chains/loaders/integrations are concretely needed), thin custom adapter (chosen default — lighter, matches the "provider SDKs only in adapters" rule with minimal surface area).
- The abstraction MUST cover at minimum: chat completion (with tool calling support), embedding generation, and streaming responses.
- Provider SDKs and API keys are configured in the application's composition root (`main.py` or `core/config.py`), keeping provider details out of business modules.

## Constraints (non-negotiable for AI)
- Service code MUST interact with LLMs through abstract interfaces (Python Protocol or ABC), NEVER through direct provider SDK calls.
- Provider-specific SDK imports MUST only appear in adapter/factory modules or the composition root. NEVER in the AI module's `service.py`.
- All LLM calls MUST be `async`. NEVER call synchronous provider SDKs in async code paths without proper wrapping.
- Model names, API keys, and endpoint URLs MUST come from configuration (environment variables or settings). NEVER hardcode provider details.
- The abstraction MUST support tool/function calling. Tool definitions MUST be passed through the abstraction, not provider-specific formats.
- The abstraction MUST support streaming responses for real-time output scenarios.
- Swapping from one provider to another MUST require changes only in the composition root or adapter module, not in service code.

## Examples

**Violation — provider SDK imported in the AI service:**
```python
# src/app/modules/ai/service.py
from anthropic import AsyncAnthropic   # provider coupling in service code

client = AsyncAnthropic()
```

**Compliant:**
```python
# src/app/modules/ai/service.py
class ChatClient(Protocol):
    async def chat(self, messages: list[ChatMessage], tools: list[ToolDef] | None = None) -> ChatResult: ...

async def answer(chat: ChatClient, question: str) -> str:  # adapter injected at composition root
    result = await chat.chat(build_messages(question))
    return result.text
```
