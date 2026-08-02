---
category: ai
stack: any
status: Active
requires:
  - adrs/ai/llm-abstraction.md
  - adrs/ai/ai-agent-module.md
  - adrs/database/uuid-primary-keys.md
  - adrs/database/timestamptz-always.md
conflicts_with: []
last_reviewed: 2026-08-01
---

# Conversation History with Token-Aware Context Management

## Decision
Multi-turn conversations are persisted in `conversations` and `messages` tables owned by the AI module. A token-aware context windowing strategy prunes or summarizes conversation history before sending it to the LLM. The system prompt and the most recent user message are always preserved; the system never sends unbounded message history to the model.

## Rationale
- Persisting conversation history allows users to resume conversations and provides an audit trail of AI interactions. Without persistence, every restart or session timeout loses context.
- Alternatives considered: in-memory conversation storage (rejected — lost on restart, no cross-instance continuity), storing full history in the client (rejected — exposes internal prompts and tool calls, payload size issues), external conversation stores like Redis or third-party services (rejected — adds infrastructure; PostgreSQL is already available).
- Token-aware windowing prevents context overflow errors and controls cost. Summarization of older messages preserves context while staying within limits.
- `user_id` is a plain UUID referencing the user's identity, consistent with the cross-module-by-id family rule — no navigation property or relationship to a User model in another module.

## Constraints (non-negotiable for AI)
- The `conversations` table MUST include: `id` (UUID), `user_id` (UUID, plain column), `title` (text, nullable), `created_at` (timestamptz), `updated_at` (timestamptz).
- The `messages` table MUST include: `id` (UUID), `conversation_id` (UUID FK), `role` (system/user/assistant/tool), `content` (text), `token_count` (integer), `created_at` (timestamptz). Optional: tool-call metadata columns.
- The system MUST track token counts per message and prune history when approaching the model's context limit. Use the provider's tokenizer when available; otherwise a single deterministic approximation (e.g., `len(text) // 4`) applied consistently — never ad-hoc estimates per call site.
- Context window limits MUST be configurable per model.
- When pruning, the system MUST always preserve the system prompt and the most recent user message.
- If summarization is used, the summary MUST be persisted as a flagged `system`-role message replacing the pruned span, so the stored history remains the single source of truth (prune-only is the acceptable default).
- Every LLM call MUST assemble its message list through a single context-builder component that applies the windowing policy — callers MUST NOT hand-assemble message lists.
- `user_id` MUST be a plain UUID with no navigation property/relationship to any User entity in another module.
- Both tables MUST be owned by the AI module's data layer (`AIDbContext` on .NET, the AI module's `models.py` in Python) and MUST NOT appear in any other module's mapping.
- NEVER send unbounded conversation history to the LLM. Every call MUST respect the configured context window.

## Examples

**Violation — hand-assembled, unbounded message list:**
```python
messages = [{"role": m.role, "content": m.content} for m in conversation.messages]
response = await llm.chat(messages)  # grows without limit
```

**Compliant (.NET):**
```csharp
var messages = _contextBuilder.Build(conversation, _options.ContextBudget);
var response = await _chat.GetResponseAsync(messages, options, ct);
// the single context builder enforces the token budget for every call
```

**Compliant (Python):**
```python
messages = build_context(conversation, token_budget=settings.llm_context_budget)
response = await llm.chat(messages)  # the single context-builder enforces the budget
```
