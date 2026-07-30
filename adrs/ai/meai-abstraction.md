---
category: ai
stack: dotnet
status: Active
requires:
  - adrs/dotnet/modular-monolith.md
  - adrs/dotnet/async-all-the-way.md
conflicts_with:
  - adrs/ai/llm-abstraction-python.md
  - adrs/ai/claude-agent-sdk.md
last_reviewed: 2026-07-29
verify_against:
  - Microsoft.Extensions.AI
---

# Microsoft.Extensions.AI as Sole LLM Abstraction

## Decision
All LLM and embedding calls go through `IChatClient` and `IEmbeddingGenerator<string, Embedding<float>>` from Microsoft.Extensions.AI (M.E.AI). Provider-specific SDKs (OpenAI, Azure OpenAI, Anthropic, Ollama, etc.) are only referenced in the composition root. Cross-cutting concerns use the M.E.AI middleware pipeline.

## Rationale
- M.E.AI provides a provider-agnostic abstraction over LLM and embedding APIs, allowing the application to swap providers without changing service code. This is the same pattern .NET uses for logging (`ILogger`), caching (`IDistributedCache`), and HTTP (`HttpClient`).
- Alternatives considered: Semantic Kernel (rejected as primary abstraction — heavier framework with its own orchestration model; can be layered on top of M.E.AI if needed), direct OpenAI SDK usage (rejected — creates hard provider coupling throughout the codebase), LangChain.NET (rejected — less mature, not aligned with Microsoft's extension pattern).
- The middleware pipeline (`ChatClientBuilder` / `EmbeddingGeneratorBuilder`) enables composable cross-cutting concerns (logging, caching, telemetry, rate limiting) without modifying service code.
- Registering provider SDKs only in the API host's composition root aligns with the thin-api-host and modular-monolith decisions.

## Constraints (non-negotiable for AI)
- All LLM interactions MUST go through `IChatClient`. NEVER call provider SDKs directly from service code.
- All embedding generation MUST go through `IEmbeddingGenerator<string, Embedding<float>>`. NEVER call provider embedding APIs directly.
- Provider SDK packages (e.g., `Microsoft.Extensions.AI.OpenAI`; for Anthropic the official `Anthropic` SDK's `IChatClient` integration; for Ollama the `OllamaSharp` package's `IChatClient` implementation — the preview `Microsoft.Extensions.AI.Ollama` package is deprecated) MUST only be referenced in the API host project, NEVER in the AI module.
- Cross-cutting concerns (logging, caching, telemetry, rate limiting) MUST be registered as M.E.AI middleware via `ChatClientBuilder` or `EmbeddingGeneratorBuilder`.
- Model names, API keys, and endpoint URLs MUST come from configuration (`IConfiguration` / `IOptions<T>`). NEVER hardcode provider details.
- The AI module MUST depend only on the `Microsoft.Extensions.AI.Abstractions` package, not on any provider-specific package.

## Examples

**Violation — provider SDK used directly in service code:**
```csharp
private readonly OpenAIClient _openAi; // provider type inside the AI module
var completion = await _openAi.GetChatClient(model).CompleteChatAsync(prompt);
```

**Compliant:**
```csharp
private readonly IChatClient _chat;    // M.E.AI abstraction only
var response = await _chat.GetResponseAsync(messages, options, ct);
// the concrete provider is registered in the API host's composition root
```
