# ADR-002: PostgreSQL + pgvector vs. Base de Datos Vectorial Dedicada

## Estado
**Aceptado** — 2025

## Contexto

El módulo AI Concierge requiere almacenamiento y búsqueda de vectores de alta dimensión (embeddings de documentos) para implementar el pipeline RAG. Existen dos familias de soluciones para este problema: bases de datos vectoriales dedicadas y extensiones vectoriales sobre bases de datos relacionales existentes.

El sistema ya requiere PostgreSQL para datos relacionales (Tenants, Documentos, Sesiones de Chat, Usuarios). La decisión determina si se añade un segundo motor de base de datos al stack o si se extiende el existente.

## Opciones Consideradas

### Opción A: Base de Datos Vectorial Dedicada
Soluciones como **Qdrant**, **Weaviate** o **Pinecone** están diseñadas específicamente para búsqueda vectorial a escala.

**Pros:**
- Rendimiento óptimo en búsquedas vectoriales a muy alta escala (millones de vectores).
- Índices especializados (HNSW nativo, filtros avanzados).
- API dedicada y SDKs maduros.

**Contras:**
- Introduce un segundo servicio en la infraestructura (contenedor adicional, configuración de red, respaldo separado).
- El usuario objetivo (PyME) debe gestionar y mantener dos bases de datos.
- Duplica la complejidad operativa de Docker Compose y los planes de disaster recovery.
- La sincronización entre los metadatos en PostgreSQL y los vectores en la DB dedicada requiere lógica adicional y es una fuente potencial de inconsistencias.
- Costo adicional si se usa como servicio cloud administrado (Pinecone).

### Opción B: PostgreSQL + pgvector (Seleccionada)
La extensión `pgvector` añade el tipo de dato `vector`, índices HNSW e IVFFlat, y funciones de distancia (coseno, L2, producto interno) directamente en PostgreSQL.

**Pros:**
- Un solo motor de base de datos para datos relacionales y vectoriales: menor TCO, respaldo unificado, un solo servicio en Docker Compose.
- Los embeddings viven en la misma transacción que los metadatos del documento, garantizando consistencia sin lógica de sincronización adicional.
- El filtro por `TenantId` se aplica en la misma query SQL, sin lógica especial de aislamiento entre sistemas.
- PostgreSQL 16 con `pgvector` es suficiente para las cargas esperadas en PyMEs (decenas de miles de chunks, no millones).
- Confiabilidad y madurez probadas de PostgreSQL.

**Contras:**
- Rendimiento inferior al de soluciones dedicadas a escala de millones de vectores (no aplica para el segmento PyME objetivo).
- La indexación HNSW requiere configuración cuidadosa de parámetros (`m`, `ef_construction`) para balancear velocidad y precisión.

## Decisión

Se adopta **PostgreSQL 16+ con la extensión `pgvector`** como único motor de almacenamiento para datos relacionales y vectoriales.

La tabla de chunks (`document_chunks`) incluirá una columna de tipo `vector(1536)` (dimensión del modelo `text-embedding-3-small`). Se creará un índice HNSW sobre esta columna para búsquedas por similitud coseno. Todas las queries de recuperación incluirán obligatoriamente el filtro `tenant_id` como condición WHERE antes de calcular la similitud.

## Consecuencias

- **Positivas:** Stack de infraestructura mínimo. Consistencia transaccional garantizada entre metadatos y vectores. El aislamiento multi-tenant es una condición de la query SQL, auditable y testeable.
- **A gestionar:** Se debe habilitar la extensión `pgvector` en el script de inicialización de la base de datos (`CREATE EXTENSION IF NOT EXISTS vector`). Las migraciones de EF Core deben gestionar esta columna con anotaciones o configuración Fluent API apropiada.
- **Límite conocido:** Si en el futuro un tenant supera ~500,000 chunks, se debe evaluar la migración del almacenamiento vectorial a una solución dedicada. Este umbral es prácticamente inalcanzable para una PyME en el corto y mediano plazo.
