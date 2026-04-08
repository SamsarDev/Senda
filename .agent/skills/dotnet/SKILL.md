# Skill: Dotnet Mastery

This skill provides the necessary instructions, patterns, and tools to work efficiently with the Senda ecosystem, which is built on .NET 10, Clean Architecture, and DDD.

## Core Principles

- **Clean Architecture**: Dependency flows inward. The core contains business logic and domain entities and has no dependencies on other layers.
- **DDD (Domain-Driven Design)**: Use entities, value objects, and domain services to model business logic.
- **Multi-Tenancy**: Every business entity must implement `ITenantEntity` to ensure data isolation.
- **Async First**: Use `Task` and `await` for all I/O bound operations.
- **Functional validation**: Use FluentValidation for all commands and DTOs.

## Project Structure

- `Senda.Core`: Entities, Enums, Domain Services, Repositories (Interfaces).
- `Senda.Application`: Use cases, Commands, Queries (MediatR), DTOs, Validators.
- `Senda.Infrastructure`: Database context, External services implementations (Semantic Kernel, PostgreSql).
- `Senda.Api`: Controllers, Middleware, API configuration.

## Common Commands

### Build and Run
- Build: `dotnet build Senda.slnx`
- Run API: `dotnet run --project src/Senda.Api`

### Database (EF Core)
- Add Migration: `dotnet ef migrations add <Name> --project src/Senda.Infrastructure --startup-project src/Senda.Api`
- Update Database: `dotnet ef database update --project src/Senda.Infrastructure --startup-project src/Senda.Api`

## Multi-Tenancy Implementation

When adding new entities:
1. Ensure the entity has a `TenantId` property.
2. Implement `ITenantEntity` (create it in `Senda.Core/Interfaces` if not present).
3. Configure `SendaDbContext` to apply a global query filter for `TenantId`.

## Semantic Search & Vectors

- Use `pgvector` for semantic search.
- Vectors in C# are handled via `float[]` or specific pgvector types.
- Dimension for `text-embedding-3-small` is **1536**.

## Error Handling

- Avoid throwing generic exceptions.
- Use domain-specific exceptions located in `Senda.Core/Exceptions`.
- The API uses a global exception handler to map these to appropriate HTTP status codes.
