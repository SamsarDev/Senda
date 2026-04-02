# ADR-004: Estrategia de Multi-tenancy

## Estado
**Aceptado** — 2025

## Contexto

Senda debe soportar múltiples negocios (tenants) desde una única instancia del sistema. El aislamiento de datos entre tenants es un requisito de seguridad crítico: bajo ninguna circunstancia la información de un tenant debe ser accesible por otro, especialmente los vectores de conocimiento y el historial de conversaciones.

Existen tres estrategias principales de multi-tenancy para bases de datos relacionales, cada una con implicancias distintas en seguridad, costo operativo y complejidad.

## Opciones Consideradas

### Opción A: Database-per-Tenant
Cada tenant tiene su propia base de datos PostgreSQL. La aplicación selecciona la cadena de conexión dinámicamente según el tenant autenticado.

**Pros:**
- Aislamiento máximo: imposible el cruce de datos a nivel de base de datos.
- Respaldo y restauración independiente por tenant.
- Cumplimiento normativo más sencillo (cada DB puede residir en una región diferente).

**Contras:**
- Costo operativo inaceptable para el segmento PyME: N bases de datos = N veces el costo de almacenamiento y gestión.
- Las migraciones de esquema deben aplicarse a cada base de datos individualmente.
- Incompatible con el objetivo de despliegue simple (Docker Compose con una sola DB).

### Opción B: Schema-per-Tenant
Un solo servidor PostgreSQL con un schema separado por tenant (`tenant_abc.documents`, `tenant_xyz.documents`).

**Pros:**
- Mejor aislamiento que Row-Level sin el costo de múltiples bases de datos.
- Las migraciones pueden ejecutarse por schema.

**Contras:**
- EF Core tiene soporte limitado y complejo para schema dinámico por tenant.
- La cantidad de schemas puede crecer rápidamente y dificultar la administración.
- Semantic Kernel y pgvector no están diseñados con este patrón en mente.

### Opción C: Row-Level Isolation con TenantId (Seleccionada)
Una única base de datos y schema compartidos. Todas las tablas incluyen una columna `tenant_id`. El aislamiento se garantiza mediante un filtro obligatorio en todas las queries, implementado en la capa de repositorio.

**Pros:**
- Un solo esquema de base de datos: migraciones simples y unificadas.
- Compatible con EF Core Global Query Filters (`HasQueryFilter`), que aplican el filtro `TenantId` automáticamente en todas las queries de un `DbContext`.
- Compatible con pgvector: el filtro `tenant_id` se incluye en la misma query de búsqueda vectorial.
- Despliegue simple: una sola instancia de PostgreSQL.
- Escalable horizontalmente con read replicas sin complejidad adicional.

**Contras:**
- El aislamiento depende de la correcta implementación del filtro en cada repositorio; un bug puede exponer datos de otros tenants. **Este riesgo se mitiga con EF Core Global Query Filters y tests de integración obligatorios que validen el aislamiento.**
- Todos los tenants comparten el mismo espacio de almacenamiento; un tenant con volumen muy alto puede impactar el rendimiento de otros (mitigado con índices por `tenant_id`).

## Decisión

Se adopta la estrategia de **Row-Level Isolation con `TenantId`** como mecanismo de multi-tenancy.

### Implementación definida:

1. **`ITenantContext`** — Interfaz en `Senda.Core` que expone el `TenantId` del tenant autenticado actualmente. Implementada en `Senda.Infrastructure` resolviendo el valor desde el JWT claim.

2. **`AppDbContext`** — Configura un `Global Query Filter` en `OnModelCreating` para cada entidad que implementa `ITenantEntity`:
   ```csharp
   builder.Entity<T>().HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
   ```

3. **Regla obligatoria:** Ninguna entidad de dominio que contenga datos de negocio puede existir sin la propiedad `TenantId`. Esto se hace cumplir mediante la interfaz `ITenantEntity` y validaciones en code review.

4. **Vectores:** La tabla `document_chunks` incluye `tenant_id` y todas las queries de similitud en pgvector incluyen `WHERE tenant_id = @tenantId` como condición previa al ranking por distancia.

5. **Tests de aislamiento:** Se implementarán tests de integración que verifican explícitamente que un tenant no puede acceder a datos de otro, incluso si conoce el ID del recurso.

## Consecuencias

- **Positivas:** Despliegue simple. Migraciones unificadas. Compatibilidad total con EF Core y pgvector.
- **A gestionar:** Los Global Query Filters de EF Core tienen un método de escape (`IgnoreQueryFilters()`). Su uso debe estar prohibido salvo en contextos administrativos explícitamente definidos y documentados. Se debe añadir una regla de análisis estático o code review checklist para detectar su uso no autorizado.
- **Deuda técnica aceptada:** Si en el futuro un tenant requiere cumplimiento normativo estricto (ej. GDPR con residencia de datos), se evaluará migrar ese tenant específico a una instancia dedicada con Database-per-Tenant.
