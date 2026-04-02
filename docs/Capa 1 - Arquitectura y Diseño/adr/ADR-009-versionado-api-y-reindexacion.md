# ADR-009: Versionado de API y Estrategia de Re-indexación de Documentos

## Estado
**Aceptado** — 2025

---

## Parte 1: Versionado de la API REST

### Contexto

Senda expondrá una API REST pública que será consumida por el dashboard de Vue 3, los widgets de chat embebidos, y potencialmente por integraciones de terceros (WhatsApp, Telegram). Al ser un proyecto open source, es probable que existan instancias en producción que no se actualicen de forma inmediata. Se necesita una estrategia de versionado que permita evolucionar la API sin romper integraciones existentes.

### Opciones Consideradas

**Opción A: Header Versioning (`Api-Version: 1.0`)**
La versión se especifica en un header HTTP personalizado.

- **Pros:** URLs limpias sin versión.
- **Contras:** No visible en el browser, difícil de probar con `curl` sin flags adicionales, menos intuitivo para consumidores de API. Más complejo de documentar en OpenAPI.

**Opción B: Query String Versioning (`/api/chat?version=1`)**
La versión va como parámetro de query.

- **Pros:** Simple de implementar.
- **Contras:** Se mezcla con los parámetros de negocio de cada endpoint. Semánticamente incorrecto (la versión no es un filtro del recurso).

**Opción C: URI Versioning `/api/v1/` (Seleccionada)**
La versión forma parte del path del recurso.

- **Pros:** Visible e inequívoca en la URL. Fácil de probar, cachear y documentar. Estándar de facto en APIs open source populares (GitHub API, Stripe API). Soporte nativo excelente en .NET con el paquete `Asp.Versioning.Mvc`.
- **Contras:** Proliferación de rutas si hay muchas versiones activas simultáneamente (gestionable con políticas de deprecación claras).

### Decisión — Versionado

Se adopta **URI Versioning** con el prefijo `/api/v{version}/`.

**Política de versiones:**
- La versión actual del MVP es `v1`. Todos los endpoints siguen el patrón `/api/v1/{recurso}`.
- Una versión se declara **deprecada** cuando existe una versión superior estable. Se añade el header `Deprecation: true` en las respuestas de la versión deprecada.
- Una versión deprecada se mantiene activa durante **6 meses** antes de ser eliminada, con aviso en el CHANGELOG.
- El paquete `Asp.Versioning.Mvc` se configura en `Senda.API` para gestionar el routing por versión.

**Formato de respuesta de error estandarizado (todas las versiones):**
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Validation Error",
  "status": 400,
  "errors": {
    "field": ["El campo es requerido."]
  },
  "traceId": "00-abc123-def456-00"
}
```
Se usará el estándar **RFC 7807 Problem Details** (`ProblemDetails` de ASP.NET Core) para todos los errores.

---

## Parte 2: Estrategia de Re-indexación de Documentos

### Contexto

Cuando un administrador actualiza un documento (sube una nueva versión de un PDF), el sistema debe decidir cómo gestionar los vectores de la versión anterior. Los chunks desactualizados en la base de datos vectorial pueden causar respuestas incorrectas o desactualizadas.

Adicionalmente, si el modelo de embedding cambia en una versión futura del sistema (ver ADR-008), todos los documentos de todos los tenants deben ser re-indexados.

### Opciones Consideradas

**Opción A: Re-indexación Delta / Incremental**
Detectar qué secciones cambiaron y solo re-indexar esas.

- **Pros:** Eficiente en tiempo y costo para documentos grandes.
- **Contras:** Implementación muy compleja para PDFs (requiere diff semántico, no diff de texto). Fuente de bugs e inconsistencias. Inapropiado para MVP.

**Opción B: Versionado de Documentos con Activación Manual**
Mantener múltiples versiones de un documento en la DB. El administrador activa la nueva versión cuando está listo.

- **Pros:** Rollback posible. Sin downtime del conocimiento durante la re-indexación.
- **Contras:** Mayor complejidad de modelo de datos. Mayor uso de almacenamiento. La UI debe manejar el concepto de "versión activa".

**Opción C: Re-indexación Completa con `ProcessingStatus` (Seleccionada)**
Al subir una nueva versión, se eliminan todos los chunks del documento anterior y se re-procesan desde cero. El progreso es visible mediante el campo `ProcessingStatus` de la entidad `Document`.

- **Pros:** Simple, garantiza consistencia completa, sin lógica de diff compleja. El administrador puede ver el estado del proceso en el dashboard.
- **Contras:** Durante la re-indexación, el documento no está disponible para consultas (~segundos a minutos dependiendo del tamaño). Aceptable para el MVP.

### Decisión — Re-indexación

Se adopta la estrategia de **Re-indexación Completa con `ProcessingStatus`**.

**Estados del ciclo de vida de un Document:**

```
Uploaded → Pending → Processing → Indexed
                         ↓
                       Failed
```

| Estado | Descripción |
|---|---|
| `Uploaded` | El archivo ha sido recibido y almacenado, pero aún no encolado para procesamiento. |
| `Pending` | Encolado para procesamiento. |
| `Processing` | Extracción de texto, chunking y generación de embeddings en curso. |
| `Indexed` | Todos los chunks han sido generados y almacenados en pgvector. Disponible para consultas RAG. |
| `Failed` | El procesamiento falló. El campo `ProcessingError` contiene el detalle del error. El administrador puede reintentar. |

**Proceso de actualización de documento:**
1. El administrador sube la nueva versión del archivo.
2. El sistema crea un nuevo registro `Document` con estado `Pending`.
3. En background (hosted service), se procesan los chunks: se eliminan los chunks del documento anterior (`WHERE document_id = @oldId`) y se insertan los nuevos.
4. El estado del nuevo documento pasa a `Indexed`. El registro anterior se marca como `Superseded`.

**Re-indexación masiva (cambio de modelo de embedding):**
Se implementará un endpoint administrativo protegido `POST /api/v1/admin/reindex` que re-encola todos los documentos de un tenant (o de todos los tenants) para re-indexación. Este endpoint requiere el rol `SystemAdmin` (diferente del `TenantAdmin`).

## Consecuencias

- **Positivas (Versionado):** API predecible y estable para integraciones de terceros. Política de deprecación clara para el ecosistema open source.
- **Positivas (Re-indexación):** Consistencia garantizada. Estado visible en el dashboard. Sin lógica de diff compleja.
- **A gestionar:** El procesamiento de documentos debe ocurrir en un `BackgroundService` de .NET para no bloquear el request HTTP de la subida. Se debe implementar manejo de errores robusto con reintentos configurables.
- **Post-MVP (Re-indexación):** Evaluar la implementación de un sistema de cola de mensajes (ej. RabbitMQ o Redis Streams) para gestionar el procesamiento de documentos de forma más resiliente cuando el volumen de tenants crezca.
