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
  - adrs/ai/rag-pgvector-python.md
last_reviewed: 2026-07-29
---

# RAG Pipeline with pgvector

## Decision
Retrieval-augmented generation (RAG) uses `IEmbeddingGenerator` from M.E.AI for embedding generation and PostgreSQL with the pgvector extension for vector storage and similarity search. The pipeline follows: chunk documents → generate embeddings → store in vector table → at query time, embed the user query → perform similarity search → inject retrieved context into the LLM prompt.

## Rationale
- pgvector keeps vector storage co-located with the application's existing PostgreSQL database, eliminating the operational overhead of a separate vector database. Since the stack already uses PostgreSQL, adding pgvector is a natural extension.
- Alternatives considered: dedicated vector databases such as Pinecone, Qdrant, or Weaviate (rejected — adds infrastructure complexity for current scale; can migrate later if needed), storing embeddings in a separate service (rejected — unnecessary network hop and operational burden), in-memory vector search (rejected — does not survive restarts, does not scale).
- Using `IEmbeddingGenerator` for embeddings maintains provider independence: the same interface works whether the backing model is OpenAI, Azure OpenAI, or a local model.
- Embedding tables are owned by the AI module's `AIDbContext`, consistent with the DbContext-per-module decision.

## Constraints (non-negotiable for AI)
- The pgvector extension MUST be enabled in PostgreSQL (`CREATE EXTENSION IF NOT EXISTS vector`).
- EF Core vector mapping MUST use the `Pgvector` and `Pgvector.EntityFrameworkCore` NuGet packages (`UseVector()` on the Npgsql data source / model configuration).
- The embedding table MUST include at minimum: `id` (UUID), `content` (text), `embedding` (vector(N)), `source_reference` (text), `created_at` (timestamptz).
- Vector dimensionality MUST be configurable via settings. NEVER hardcode the dimension size.
- Similarity search MUST use cosine distance and a pgvector index (IVFFlat or HNSW).
- Top-k results MUST be configurable (default: 5). Retrieved context MUST be wrapped in explicit delimiters in the prompt (e.g., an XML tag such as `<retrieved_context>` per chunk, including its source reference).
- Chunk size and overlap MUST be configurable via settings.
- Embedding tables MUST be mapped in `AIDbContext` and MUST NOT appear in any other module's DbContext.
- All embedding generation MUST go through `IEmbeddingGenerator`, NEVER through direct provider SDK calls.
