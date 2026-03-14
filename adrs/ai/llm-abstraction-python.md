# Provider-Agnostic LLM Abstraction Layer

**Category:** ai
**Status:** Active
**Requires:** `adrs/python/async-all-the-way.md`, `adrs/python/service-layer-logic.md`
**Conflicts with:** `adrs/ai/meai-abstraction.md`

## Decision
All LLM and embedding calls go through a provider-agnostic abstraction layer. Service code depends on abstract interfaces (protocols or ABC classes) for chat completion and embedding generation. Provider-specific SDKs (Anthropic, OpenAI, etc.) are only imported in the composition root or adapter modules. LangChain may be used as the abstraction layer, or a thin custom adapter — but service code never imports provider SDKs directly.

## Rationale
- A provider-agnostic layer allows swapping LLM providers (OpenAI, Anthropic, Azure OpenAI, local models) without changing service code. This is critical for an AI-first application where provider capabilities, pricing, and availability evolve rapidly.
- Alternatives considered: direct Anthropic/OpenAI SDK usage throughout (rejected — creates hard provider coupling, makes switching expensive), LangChain as the sole abstraction (acceptable — provides provider-agnostic interfaces, chain orchestration, and tool calling; heavier but well-maintained), custom thin adapter (acceptable — lighter but requires more manual work).
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
