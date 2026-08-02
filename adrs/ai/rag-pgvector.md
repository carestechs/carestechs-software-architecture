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
verify_against:
  - Pgvector.EntityFrameworkCore
  - pgvector (Python package)
---

# RAG Pipeline with pgvector

## Decision
Retrieval-augmented generation uses the provider-agnostic embedding abstraction for embedding generation and PostgreSQL with the pgvector extension for vector storage and similarity search. The pipeline: chunk documents → generate embeddings → store in a vector table owned by the AI module → at query time, embed the user query → similarity search → inject retrieved context into the LLM prompt.

## Rationale
- pgvector keeps vector storage co-located with the application's existing PostgreSQL database, eliminating the operational overhead of a separate vector database.
- Alternatives considered: dedicated vector databases such as Pinecone, Qdrant, or Weaviate (rejected — infrastructure complexity for current scale; can migrate later), ChromaDB / separate embedding services (rejected — a second data store and network hop when PostgreSQL already covers the need), in-memory vector search or FAISS (rejected — does not survive restarts, does not scale).
- Generating embeddings through the abstraction keeps provider independence: the same interface works for OpenAI, Azure OpenAI, or a local model.
- Embedding tables are owned by the AI module, consistent with per-module data ownership.

## Constraints (non-negotiable for AI)
- The pgvector extension MUST be enabled in PostgreSQL (`CREATE EXTENSION IF NOT EXISTS vector`).
- The embedding table MUST include at minimum: `id` (UUID), `content` (text), `embedding` (vector(N)), `source_reference` (text), `created_at` (timestamptz).
- Vector dimensionality MUST be configurable via settings. NEVER hardcode the dimension size.
- Similarity search MUST use cosine distance and a pgvector index (IVFFlat or HNSW).
- Top-k results MUST be configurable (default: 5). Retrieved context MUST be wrapped in explicit delimiters in the prompt (e.g., an XML tag such as `<retrieved_context>` per chunk, including its source reference).
- Chunk size and overlap MUST be configurable via settings.
- Embedding tables MUST be mapped only in the AI module's data layer — never in another module's mapping.
- All embedding generation MUST go through the abstraction (see `llm-abstraction`), NEVER through direct provider SDK calls.

**.NET mechanics:**
- EF Core vector mapping uses the `Pgvector` and `Pgvector.EntityFrameworkCore` packages (`UseVector()` on the Npgsql data source / model configuration); tables map in `AIDbContext`.

**Python mechanics:**
- SQLAlchemy column types come from the `pgvector` package (`from pgvector.sqlalchemy import Vector`); models live in the AI module's `models.py`.
