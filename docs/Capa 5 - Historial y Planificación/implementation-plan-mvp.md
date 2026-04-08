# Implementation Plan: Senda AI Concierge MVP

This document outlines the original implementation plan for Phase 1 and 2 of the Senda AI Concierge project.

## Goals
- Establish the core multi-tenant foundation.
- Implement the RAG pipeline using Semantic Kernel and pgvector.
- Build a functional administrative dashboard.

## Phases

### Phase 1: Core Foundation
- [x] Multi-tenancy interfaces and entities.
- [x] SendaDbContext with Global Query Filters.
- [x] Database migrations and pgvector setup.

### Phase 2: RAG Pipeline
- [x] ITextEmbeddingService with Ollama/Semantic Kernel.
- [x] IChatCompletionService with Ollama/Semantic Kernel.
- [x] IFileStorageService (Hybrid Local/Azure).
- [x] Text Extractors and Chunkers.

### Phase 3: API Orchestration
- [x] TenantMiddleware for X-Tenant-Id.
- [x] API Controllers (Tenants, Knowledge, Chat).

### Phase 4: Frontend MVP
- [x] Vue 3 + PrimeVue 4 + Tailwind project initialization.
- [x] Knowledge Management Dashboard.
- [x] Chat Playground UI.

---

## Technical Decisions
- **LLM**: Ollama (Qwen 3.5) via OpenAI connector.
- **Vectors**: pgvector (L2 Distance).
- **Design**: PrimeVue Noir theme for a premium look.
