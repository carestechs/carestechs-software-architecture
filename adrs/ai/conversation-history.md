---
category: ai
stack: dotnet
status: Active
requires:
  - adrs/ai/meai-abstraction.md
  - adrs/ai/ai-agent-module.md
  - adrs/database/uuid-primary-keys.md
  - adrs/database/timestamptz-always.md
conflicts_with:
  - adrs/ai/conversation-history-python.md
last_reviewed: 2026-07-29
---

# Conversation History with Token-Aware Context Management

## Decision
Multi-turn conversations are persisted in `conversations` and `messages` tables owned by the AI module's `AIDbContext`. A token-aware context windowing strategy prunes or summarizes conversation history before sending it to the LLM. The system never sends unbounded message history to the model.

## Rationale
- Persisting conversation history allows users to resume conversations and provides an audit trail of AI interactions. Without persistence, every page refresh or session timeout loses context.
- Alternatives considered: in-memory conversation storage (rejected — loses history on restart, does not scale across instances), storing full history in the client (rejected — exposes internal prompts and tool calls, creates payload size issues), third-party conversation management services (rejected — unnecessary external dependency).
- Token-aware windowing prevents context overflow errors and controls cost by ensuring only relevant history is sent to the model. Summarization of older messages preserves context while staying within token limits.
- The `user_id` column is a plain Guid referencing the user's identity, consistent with the cross-module-by-id decision — no navigation property to the User entity.

## Constraints (non-negotiable for AI)
- The `conversations` table MUST include: `id` (UUID), `user_id` (Guid), `title` (text), `created_at` (timestamptz), `updated_at` (timestamptz).
- The `messages` table MUST include: `id` (UUID), `conversation_id` (UUID FK), `role` (system/user/assistant/tool), `content` (text), `token_count` (integer), `created_at` (timestamptz). Optional: tool-call metadata columns.
- The system MUST track cumulative token counts and prune history when approaching the model's context limit.
- Context window limits MUST be configurable per model.
- When pruning, the system MUST always preserve the system prompt and the most recent user message.
- If summarization is used, the summary MUST be persisted as a flagged `system`-role message replacing the pruned span, so the stored history remains the single source of truth (prune-only is the acceptable default).
- Every LLM call MUST assemble its message list through a single context-builder component that applies the windowing policy — callers MUST NOT hand-assemble message lists.
- `user_id` MUST be a plain Guid with no navigation property to any User entity in another module.
- Both tables MUST be mapped in `AIDbContext` and MUST NOT appear in any other module's DbContext.
- NEVER send unbounded conversation history to the LLM. Every call MUST respect the configured context window.
