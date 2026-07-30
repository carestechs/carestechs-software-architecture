# RAG Pipeline with pgvector (Python)

**Category:** ai
**Status:** Active
**Requires:** `adrs/ai/llm-abstraction-python.md`, `adrs/ai/ai-module-python.md`, `adrs/python/sqlalchemy-async.md`, `adrs/database/uuid-primary-keys.md`, `adrs/database/timestamptz-always.md`
**Conflicts with:** `adrs/ai/rag-pgvector.md`

## Decision
Retrieval-augmented generation (RAG) uses the provider-agnostic embedding abstraction for embedding generation and PostgreSQL with the pgvector extension for vector storage and similarity search. The pipeline follows: chunk documents, generate embeddings, store in a vector table via SQLAlchemy, and at query time embed the user query, perform similarity search, and inject retrieved context into the LLM prompt.

## Rationale
- pgvector keeps vector storage co-located with the application's existing PostgreSQL database, eliminating the operational overhead of a separate vector database. Since the stack already uses PostgreSQL, adding pgvector is a natural extension.
- Alternatives considered: dedicated vector databases such as Pinecone, Qdrant, or Weaviate (rejected — adds infrastructure complexity for current scale; can migrate later if needed), ChromaDB (rejected — introduces a second data store when PostgreSQL with pgvector already covers the need), in-memory FAISS (rejected — does not survive restarts, does not scale).
- The `pgvector` Python package provides SQLAlchemy column types and operators for vector operations, integrating cleanly with the async SQLAlchemy stack.
- Embedding tables are owned by the AI module, consistent with modular boundaries.

## Constraints (non-negotiable for AI)
- The pgvector extension MUST be enabled in PostgreSQL (`CREATE EXTENSION IF NOT EXISTS vector`).
- The embedding SQLAlchemy model MUST include at minimum: `id` (UUID), `content` (Text), `embedding` (Vector(N)), `source_reference` (String), `created_at` (DateTime with timezone).
- Vector dimensionality MUST be configurable via settings. NEVER hardcode the dimension size.
- Similarity search MUST use cosine distance and a pgvector index (IVFFlat or HNSW).
- Top-k results MUST be configurable (default: 5). Retrieved context MUST be wrapped in explicit delimiters in the prompt (e.g., an XML tag such as `<retrieved_context>` per chunk, including its source reference).
- Chunk size and overlap MUST be configurable via settings.
- Embedding models MUST be mapped in the AI module's `models.py` and MUST NOT appear in any other module's models.
- All embedding generation MUST go through the provider-agnostic abstraction, NEVER through direct provider SDK calls.
- Use the `pgvector` Python package for SQLAlchemy column types (`from pgvector.sqlalchemy import Vector`).
