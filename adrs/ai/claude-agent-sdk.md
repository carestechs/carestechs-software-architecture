---
category: ai
stack: typescript
status: Active
requires:
  - adrs/typescript/strict-typescript.md
conflicts_with:
  - adrs/ai/meai-abstraction.md
  - adrs/ai/llm-abstraction-python.md
last_reviewed: 2026-07-29
---

# Claude Agent SDK for AI-Powered Analysis (TypeScript)

## Decision

All AI-powered analysis uses the Claude Agent SDK (`@anthropic-ai/claude-agent-sdk`) for TypeScript. The SDK is used directly — no provider-agnostic abstraction layer; a single orchestrator component is the only integration point, and no other component calls the SDK directly. AI responses are treated as untrusted input and validated before use. Choose this ADR for TypeScript CLI tools and single-provider agent products; .NET and Python service stacks use `meai-abstraction` / `llm-abstraction-python` instead.

## Rationale

- The Claude Agent SDK is purpose-built for building tool-using agents with Claude models. It handles the agent loop, conversation management, tool registration, and streaming natively (structured output is enforced on our side via Zod validation — see Constraints). Using it directly avoids a premature abstraction layer before the usage patterns are understood.
- Alternatives considered: provider-agnostic abstraction from day one (rejected — adds complexity before we know which abstractions are correct; easier to extract an interface after building with one provider), raw Anthropic API client (rejected — Agent SDK handles tool calling and conversation lifecycle that would need manual implementation), LangChain (rejected — heavy framework dependency for what is fundamentally a single-purpose agent).
- The "abstract later" strategy is deliberate: build with the concrete SDK, let patterns emerge, then extract an interface if/when a second provider is needed.

## Constraints (non-negotiable for AI)

- All AI interaction MUST go through a single orchestrator component — no other component may import or call the Agent SDK directly.
- The `ANTHROPIC_API_KEY` MUST be read from an environment variable — NEVER hardcoded, NEVER logged, NEVER included in error messages or output.
- AI responses MUST be treated as untrusted input — validate and parse structured output with Zod (or equivalent) before passing to typed consumers.
- Model identifiers MUST be configurable (via config file or env var) — NEVER hardcoded in source.
- The orchestrator MUST handle API errors (auth failure, rate limiting, timeout) gracefully — surface clear error messages, never crash with raw SDK exceptions.
- Token usage MUST be tracked per run — log input/output token counts for cost awareness.
