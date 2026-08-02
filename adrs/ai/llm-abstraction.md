---
category: ai
stack: any
status: Active
requires:
  - adrs/dotnet/async-all-the-way.md | adrs/python/async-all-the-way.md
conflicts_with:
  - adrs/ai/claude-agent-sdk.md
last_reviewed: 2026-08-01
verify_against:
  - Microsoft.Extensions.AI
---

# Provider-Agnostic LLM Abstraction

## Decision
All LLM and embedding calls go through the stack's provider-agnostic abstraction; provider-specific SDKs (OpenAI, Azure OpenAI, Anthropic, Ollama, etc.) are referenced only in the composition root or dedicated adapter modules. On .NET the abstraction is Microsoft.Extensions.AI (`IChatClient`, `IEmbeddingGenerator<string, Embedding<float>>`). In Python it is a thin custom adapter layer (a Protocol plus one adapter module per provider); adopt LangChain as the abstraction only when its ecosystem is concretely needed. Either way, service code never imports provider SDKs directly.

## Rationale
- A provider-agnostic layer allows swapping LLM providers without changing service code — critical while provider capabilities, pricing, and availability evolve rapidly.
- On .NET, M.E.AI is the platform-blessed pattern (analogous to `ILogger`/`HttpClient`), and its middleware pipeline (`ChatClientBuilder` / `EmbeddingGeneratorBuilder`) gives composable cross-cutting concerns. Semantic Kernel was rejected as the primary abstraction (heavier framework with its own orchestration model; can be layered on top), as were direct provider SDK usage and LangChain.NET.
- In Python, a thin Protocol-based adapter was chosen over LangChain-by-default (heavier dependency with its own orchestration model) and over direct SDK usage throughout (hard provider coupling).
- Registering provider SDKs only at the composition root aligns with the thin-host and modular boundaries decisions.

## Constraints (non-negotiable for AI)
- Service code MUST interact with LLMs and embedding models only through the abstraction. NEVER call provider SDKs directly from service code.
- Provider SDK packages/imports MUST appear only in the composition root or adapter modules — never inside the AI module's services.
- Model names, API keys, and endpoint URLs MUST come from configuration. NEVER hardcode provider details.
- The abstraction MUST support tool/function calling and streaming responses; tool definitions pass through the abstraction, not provider-specific formats.
- All LLM calls MUST be async end to end.
- Swapping providers MUST require changes only in the composition root or an adapter module.

**.NET mechanics:**
- Chat via `IChatClient`; embeddings via `IEmbeddingGenerator<string, Embedding<float>>`.
- The AI module references only `Microsoft.Extensions.AI.Abstractions`; provider packages (e.g., `Microsoft.Extensions.AI.OpenAI`; the official `Anthropic` SDK's `IChatClient` integration; `OllamaSharp` for Ollama — the preview `Microsoft.Extensions.AI.Ollama` package is deprecated) live in the API host only.
- Cross-cutting concerns (logging, caching, telemetry, rate limiting) register as M.E.AI middleware via `ChatClientBuilder`/`EmbeddingGeneratorBuilder`.

**Python mechanics:**
- Abstract interfaces are Python Protocols or ABCs covering chat completion (with tools), embedding generation, and streaming.
- One adapter module per provider; adapters and API keys are wired in the composition root (`main.py` / `core/config.py`).

## Examples

**Violation — provider SDK used directly in service code:**
```python
# src/app/modules/ai/service.py
from anthropic import AsyncAnthropic   # provider coupling in service code
client = AsyncAnthropic()
```

**Compliant (.NET):**
```csharp
private readonly IChatClient _chat;    // M.E.AI abstraction only
var response = await _chat.GetResponseAsync(messages, options, ct);
// the concrete provider is registered in the API host's composition root
```

**Compliant (Python):**
```python
class ChatClient(Protocol):
    async def chat(self, messages: list[ChatMessage], tools: list[ToolDef] | None = None) -> ChatResult: ...

async def answer(chat: ChatClient, question: str) -> str:  # adapter injected at composition root
    result = await chat.chat(build_messages(question))
    return result.text
```
