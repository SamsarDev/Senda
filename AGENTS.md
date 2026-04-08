# AGENTS.md — Senda AI Concierge

## Project Overview
Enterprise AI ecosystem for SMBs. First module: AI Concierge with RAG (Retrieval-Augmented Generation) for customer support.

## Architecture

**Stack:**
- Backend: .NET 10 (Clean Architecture, DDD)
- AI: Semantic Kernel (Microsoft)
- Database: PostgreSQL + pgvector (semantic search)
- Frontend: Vue 3 + Vite + PrimeVue + Tailwind (planned, not yet implemented)

**Layers (src/):**
```
Senda.Core/        → Entities, Enums, Interfaces (ITenantEntity, IAuditableEntity)
Senda.Application/ → Commands (MediatR), DTOs, Validators (FluentValidation)
Senda.Infrastructure/ → EF Core DbContext, Repository implementations
Senda.Api/         → ASP.NET Core API, Program.cs
```

## Key Commands

```bash
# Build
dotnet build Senda.slnx

# Run (requires PostgreSQL)
dotnet run --project src/Senda.Api

# Docker (full stack: DB + API + Ollama)
docker-compose up -d

# Migrations (EF Core)
dotnet ef migrations add <Name> --project src/Senda.Infrastructure --startup-project src/Senda.Api
```

## Database

- **Image:** `ankane/pgvector:latest` (includes pgvector extension)
- **Connection:** `Host=localhost;Database=senda_db;Username=admin;Password=senda_secure_pass`
- **Vector column:** `embedding vector(1536)` (for text-embedding-3-small)
- **Extension:** `CREATE EXTENSION vector;` (configured in SendaDbContext)

## Multi-Tenancy

All business entities implement `ITenantEntity` with `Guid TenantId`. Use `ITenantContext` to resolve the current tenant from JWT claims.

## RAG Pipeline

Document ingestion flow (`IngestDocumentCommandHandler`):
1. Validate tenant exists
2. Create `KnowledgeDocument` record (status: Processing)
3. Extract text (`ITextExtractorService`) 
4. Chunk text (`ITextChunkerService`, default: 512 tokens, 50 overlap)
5. Generate embeddings (`ITextEmbeddingService`)
6. Save `KnowledgeChunk` records with vectors
7. Update document status to Completed

## AI Configuration

Environment variables:
- `AI__Provider` = OpenAI | Ollama
- `AI__ApiKey` = API key for OpenAI
- `AI__Endpoint` = Ollama endpoint (e.g., http://localhost:11434)

## Solution File Format

Uses `.slnx` (Visual Studio Solution Explorer format), not `.sln`. Open with `code Senda.slnx` or VS Code.

## Current State

- **No test projects** exist yet
- **No frontend** (`src/Senda.Web/` referenced in docker-compose but not created)
- **Placeholder implementations** in `Senda.Application/DependencyInjection.cs` for `TextExtractorService` and `TextChunkerService` (throw `NotImplementedException`)
- **No migrations** created yet

## Important Notes

- Embedding dimension is hardcoded as 1536 (`vector(1536)`) for text-embedding-3-small
- `Program.cs` registers only DbContext; `AddApplication()` and `AddInfrastructure()` are not called yet
- EF Core uses Fluent API configuration (no data annotations)
- API runs on `http://localhost:5231` by default (configurable in `Senda.Api.http`)

## Documentation

- Architecture diagrams: `docs/Capa 1 - Arquitectura y Diseño/diagrams/` (Mermaid format)
- Domain model: `docs/Capa 1 - Arquitectura y Diseño/domain-model.md`
- Run `docker-compose up -d` to start the full stack including local Ollama
