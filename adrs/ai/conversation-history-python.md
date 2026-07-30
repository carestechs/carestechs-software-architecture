# Conversation History with Token-Aware Pruning (Python)

**Category:** ai
**Status:** Active
**Requires:** `adrs/ai/llm-abstraction-python.md`, `adrs/ai/ai-module-python.md`, `adrs/python/sqlalchemy-async.md`, `adrs/database/uuid-primary-keys.md`, `adrs/database/timestamptz-always.md`
**Conflicts with:** `adrs/ai/conversation-history.md`

## Decision
Multi-turn conversations are persisted in the AI module's database tables (`conversations` and `messages`) with token-aware context windowing. Before each LLM call, the conversation history is pruned or summarized to fit within the model's context window. The system prompt and most recent user message are always preserved.

## Rationale
- Persisting conversation history enables multi-turn interactions, follow-up questions, and context preservation across sessions. Without persistence, every interaction starts from scratch.
- Alternatives considered: in-memory conversation storage (rejected — lost on process restart, no cross-session continuity), storing full conversations in the LLM context without pruning (rejected — context overflow causes API errors or degraded quality), external conversation stores like Redis (rejected — adds infrastructure; PostgreSQL is already available).
- Token-aware pruning prevents context window overflow while preserving the most relevant context. The system prompt and latest user message are always included.
- Conversations and messages are SQLAlchemy models owned by the AI module, consistent with modular boundaries.

## Constraints (non-negotiable for AI)
- Conversations MUST be stored in a `conversations` table with at minimum: `id` (UUID), `user_id` (UUID, plain column — no relationship), `title` (String, nullable), `created_at` (DateTime with timezone), `updated_at` (DateTime with timezone).
- Messages MUST be stored in a `messages` table with at minimum: `id` (UUID), `conversation_id` (UUID FK), `role` (String: system/user/assistant/tool), `content` (Text), `token_count` (Integer, nullable), `created_at` (DateTime with timezone).
- Before every LLM call, the conversation history MUST be pruned to fit within a configurable token budget.
- The system prompt and the most recent user message MUST always be preserved during pruning — never drop these.
- Token counts MUST be tracked per message. Use the provider's tokenizer when available; otherwise a single deterministic approximation (e.g., `len(text) // 4`) applied consistently — never ad-hoc estimates per call site.
- User references in conversations MUST be plain UUIDs — no SQLAlchemy relationships to user models in other modules.
- NEVER send unbounded conversation history to the LLM. Enforce this architecturally: every LLM call assembles its message list through a single context-builder function that applies the token budget — callers MUST NOT hand-assemble message lists.
- If summarization is used, the summary MUST be persisted as a flagged `system`-role message replacing the pruned span, so the stored history remains the single source of truth (prune-only is the acceptable default).
- Conversation and message models MUST live in the AI module's `models.py`. They MUST NOT appear in other modules.
