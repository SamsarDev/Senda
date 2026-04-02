# Registro de Decisiones de Arquitectura (ADRs)
## Senda — Ecosistema AI para PyMEs

Este directorio contiene el registro de todas las decisiones de arquitectura significativas tomadas durante el desarrollo del ecosistema Senda. Cada ADR documenta el contexto, las opciones evaluadas, la decisión tomada y sus consecuencias.

### ¿Qué es un ADR?

Un **Architecture Decision Record** es un documento corto que captura una decisión arquitectónica importante junto con el contexto y el razonamiento que la originó. Su objetivo es que cualquier desarrollador — incluyendo el autor meses después — pueda entender *por qué* el sistema está construido de cierta manera, no solo *cómo*.

### Estados posibles de un ADR

| Estado | Descripción |
|---|---|
| `Propuesto` | En discusión, aún no aceptado. |
| `Aceptado` | Decisión tomada y vigente. |
| `Deprecado` | Fue aceptado pero ya no aplica. |
| `Reemplazado` | Sustituido por otro ADR (se indica cuál). |

---

## Índice

### Arquitectura General del Sistema

| ADR | Título | Estado |
|---|---|---|
| [ADR-001](./ADR-001-monolito-modular-vs-microservicios.md) | Monolito Modular vs. Microservicios | ✅ Aceptado |
| [ADR-002](./ADR-002-postgresql-pgvector-vs-db-vectorial-dedicada.md) | PostgreSQL + pgvector vs. Base de Datos Vectorial Dedicada | ✅ Aceptado |
| [ADR-003](./ADR-003-semantic-kernel-vs-alternativas.md) | Semantic Kernel vs. Alternativas de Orquestación de IA | ✅ Aceptado |
| [ADR-004](./ADR-004-estrategia-multi-tenancy.md) | Estrategia de Multi-tenancy | ✅ Aceptado |
| [ADR-005](./ADR-005-autenticacion-jwt.md) | Autenticación con JWT y ASP.NET Core Identity | ✅ Aceptado |
| [ADR-006](./ADR-006-cqrs-con-mediatr.md) | CQRS con MediatR vs. Application Services Simples | ✅ Aceptado |

### Módulo: AI Concierge

| ADR | Título | Estado |
|---|---|---|
| [ADR-007](./ADR-007-estrategia-chunking-documentos.md) | Estrategia de Chunking de Documentos | ✅ Aceptado |
| [ADR-008](./ADR-008-modelo-de-embedding.md) | Selección del Modelo de Embedding | ✅ Aceptado |
| [ADR-009](./ADR-009-versionado-api-y-reindexacion.md) | Versionado de API y Estrategia de Re-indexación | ✅ Aceptado |

---

## Cómo añadir un nuevo ADR

1. Copia la plantilla de `ADR-000-plantilla.md`.
2. Nómbralo con el siguiente número secuencial: `ADR-NNN-titulo-en-kebab-case.md`.
3. Completa todas las secciones. La sección **Opciones Consideradas** es obligatoria — un ADR sin alternativas evaluadas no es útil.
4. Añade la entrada al índice de este archivo.
5. Abre un PR con el ADR como único cambio para facilitar la revisión.

### Plantilla de ADR

```markdown
# ADR-NNN: Título de la Decisión

## Estado
**Propuesto** | **Aceptado** | **Deprecado** | **Reemplazado por ADR-NNN**

## Contexto
¿Qué problema o situación motivó esta decisión? ¿Cuáles son las restricciones?

## Opciones Consideradas

### Opción A: ...
**Pros:** ...
**Contras:** ...

### Opción B: ... (Seleccionada)
**Pros:** ...
**Contras:** ...

## Decisión
¿Qué se decidió y por qué?

## Consecuencias
¿Qué implica esta decisión? ¿Qué deuda técnica se acepta?
```
