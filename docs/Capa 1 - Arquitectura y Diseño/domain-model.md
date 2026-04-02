# Modelo de Dominio — Senda AI Concierge
**Versión:** 1.0.0  
**Módulo:** Senda.Core.AIConcierge  
**Estado:** Aprobado para implementación (MVP)

---

## 1. Introducción

Este documento define el modelo de dominio del módulo **Senda AI Concierge**. Describe las entidades, sus propiedades, invariantes de negocio y las relaciones entre ellas. Es la fuente de verdad para el diseño del esquema de base de datos (EF Core), las interfaces de repositorio y los casos de uso de la capa `Senda.Application`.

El modelo sigue los principios de **Domain-Driven Design (DDD)**:
- Las entidades con identidad propia son clases con `Id`.
- Los **Value Objects** son inmutables y se comparan por valor, no por referencia.
- Cada **Aggregate Root** es el único punto de entrada para modificar su agregado.
- Las **invariantes de negocio** se hacen cumplir dentro de la entidad, no en los handlers.

---

## 2. Contextos Delimitados (Bounded Contexts)

El ecosistema Senda está organizado en contextos delimitados que se corresponden con los módulos del sistema:

```
┌─────────────────────────────────────────────────────────────────┐
│                    Senda.Core (Compartido)                       │
│  ITenantEntity · ITenantContext · IAuditableEntity              │
└────────────────────────┬────────────────────────────────────────┘
                         │
        ┌────────────────┼────────────────┐
        ▼                ▼                ▼
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│  AIConcierge │  │   Loyalty    │  │  Micro ERP   │
│  (Fase 1)    │  │  (Fase 2)    │  │  (Fase 3)    │
└──────────────┘  └──────────────┘  └──────────────┘
```

Este documento cubre exclusivamente el contexto **AIConcierge**.

---

## 3. Interfaces Base (Senda.Core)

Estas interfaces se definen en `Senda.Core` y son compartidas por todos los módulos.

### `ITenantEntity`
Garantiza que toda entidad de negocio esté asociada a un tenant. Es el mecanismo que habilita los Global Query Filters de EF Core.

```csharp
public interface ITenantEntity
{
    Guid TenantId { get; }
}
```

### `IAuditableEntity`
Añade trazabilidad de creación y modificación a cualquier entidad.

```csharp
public interface IAuditableEntity
{
    DateTime CreatedAt { get; }
    DateTime? UpdatedAt { get; }
}
```

### `ITenantContext`
Expone el `TenantId` del tenant autenticado en el request actual. Se implementa en `Senda.Infrastructure` resolviendo el claim JWT.

```csharp
public interface ITenantContext
{
    Guid TenantId { get; }
    bool IsAuthenticated { get; }
}
```

---

## 4. Entidades del Dominio

### 4.1 Tenant
**Tipo:** Aggregate Root  
**Proyecto:** `Senda.Core.AIConcierge`

Representa una empresa o profesional independiente que usa el sistema. Es el núcleo del modelo de multi-tenancy. Todas las demás entidades pertenecen a un Tenant.

```csharp
public class Tenant : IAuditableEntity
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }           // Nombre del negocio. Ej: "Clínica Dental Pérez"
    public string Slug { get; private set; }           // Identificador URL-friendly. Ej: "clinica-dental-perez"
    public bool IsActive { get; private set; }         // Permite desactivar un tenant sin eliminarlo
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Relaciones (navigation properties)
    public IReadOnlyCollection<User> Users { get; }
    public IReadOnlyCollection<Document> Documents { get; }
    public IReadOnlyCollection<ChatSession> ChatSessions { get; }
    public IReadOnlyCollection<ChatChannel> ChatChannels { get; }
    public KnowledgeConfiguration? KnowledgeConfiguration { get; }
}
```

**Invariantes:**
- `Name` no puede ser nulo ni vacío.
- `Slug` debe ser único en todo el sistema. Se genera automáticamente a partir del `Name` al crear el tenant.
- Un `Tenant` desactivado (`IsActive = false`) no puede recibir nuevas sesiones de chat ni subir documentos.

---

### 4.2 User
**Tipo:** Entity  
**Proyecto:** `Senda.Core.AIConcierge`

Representa un administrador del tenant. La autenticación y el hash de contraseña son gestionados por ASP.NET Core Identity (`IdentityUser`). Esta entidad extiende Identity para añadir el vínculo con el `Tenant` y la lógica de dominio.

```csharp
public class User : IdentityUser<Guid>, ITenantEntity, IAuditableEntity
{
    public Guid TenantId { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public UserRole Role { get; private set; }         // Enum: TenantAdmin, TenantViewer
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Relaciones
    public Tenant Tenant { get; }
    public IReadOnlyCollection<RefreshToken> RefreshTokens { get; }
}
```

**Value Object — `UserRole`:**
```csharp
public enum UserRole
{
    TenantAdmin,   // Puede gestionar documentos, canales y ver historial de chats
    TenantViewer   // Solo puede ver el historial de chats (post-MVP)
}
```

**Invariantes:**
- Un `User` siempre debe tener un `TenantId` válido.
- Un `User` desactivado no puede autenticarse.

---

### 4.3 RefreshToken
**Tipo:** Entity  
**Proyecto:** `Senda.Core.AIConcierge`

Representa un token de renovación para el flujo JWT definido en ADR-005. Se almacena en la base de datos para poder revocarlo.

```csharp
public class RefreshToken : ITenantEntity
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid TenantId { get; private set; }         // Para Global Query Filter
    public string Token { get; private set; }          // Token opaco (GUID aleatorio o cadena criptográfica)
    public DateTime ExpiresAt { get; private set; }    // 7 días desde la emisión
    public bool IsRevoked { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string? RevokedReason { get; private set; } // "Logout", "Replaced", "Suspicious"

    // Relaciones
    public User User { get; }
}
```

**Invariantes:**
- Un `RefreshToken` solo puede ser revocado una vez.
- Un `RefreshToken` expirado se considera inválido aunque `IsRevoked` sea `false`.

---

### 4.4 KnowledgeConfiguration
**Tipo:** Entity (1 a 1 con Tenant)  
**Proyecto:** `Senda.Core.AIConcierge`

Almacena la configuración del comportamiento de la IA para un tenant específico. Incluye el System Prompt y los parámetros del modelo de lenguaje.

```csharp
public class KnowledgeConfiguration : ITenantEntity, IAuditableEntity
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string SystemPrompt { get; private set; }   // Ej: "Eres el asistente de Clínica X. Responde de forma concisa."
    public string LlmModel { get; private set; }       // Ej: "gpt-4o-mini", "gpt-4o", "llama3"
    public float Temperature { get; private set; }     // 0.0 - 1.0. Default: 0.3 (respuestas más deterministas para RAG)
    public int MaxResponseTokens { get; private set; } // Límite de tokens en la respuesta. Default: 500
    public int MaxContextChunks { get; private set; }  // Número de chunks del RAG a inyectar en el prompt. Default: 5
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Relaciones
    public Tenant Tenant { get; }
}
```

**Invariantes:**
- `SystemPrompt` no puede ser nulo ni vacío. Se inicializa con un prompt por defecto al crear el tenant.
- `Temperature` debe estar en el rango [0.0, 1.0].
- `MaxContextChunks` debe estar en el rango [1, 10].
- Solo puede existir una `KnowledgeConfiguration` activa por tenant.

---

### 4.5 Document
**Tipo:** Aggregate Root  
**Proyecto:** `Senda.Core.AIConcierge`

Representa un archivo subido por el administrador del tenant. Gestiona su propio ciclo de vida de procesamiento (indexación RAG).

```csharp
public class Document : ITenantEntity, IAuditableEntity
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string FileName { get; private set; }           // Nombre original del archivo. Ej: "menu_2024.pdf"
    public string StoragePath { get; private set; }        // Ruta en el sistema de archivos o blob storage
    public DocumentFileType FileType { get; private set; } // Enum: Pdf, Txt
    public long FileSizeBytes { get; private set; }
    public DocumentProcessingStatus Status { get; private set; }
    public string? ProcessingError { get; private set; }   // Detalle del error si Status = Failed
    public int ChunkCount { get; private set; }            // Total de chunks generados. 0 hasta que Status = Indexed
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? IndexedAt { get; private set; }       // Timestamp de cuando Status pasó a Indexed

    // Relaciones
    public Tenant Tenant { get; }
    public IReadOnlyCollection<DocumentChunk> Chunks { get; }
}
```

**Value Objects:**

```csharp
public enum DocumentFileType
{
    Pdf,
    Txt
}

public enum DocumentProcessingStatus
{
    Uploaded,    // Archivo recibido y almacenado. Aún no encolado.
    Pending,     // Encolado para procesamiento en background.
    Processing,  // Extracción, chunking y embedding en curso.
    Indexed,     // Todos los chunks generados. Disponible para RAG.
    Failed,      // El procesamiento falló. Ver ProcessingError.
    Superseded   // Reemplazado por una versión más nueva del mismo documento.
}
```

**Métodos de dominio (comportamiento):**
```csharp
public void MarkAsPending()         // Uploaded → Pending
public void MarkAsProcessing()      // Pending → Processing
public void MarkAsIndexed(int chunkCount)  // Processing → Indexed
public void MarkAsFailed(string error)     // Processing → Failed
public void MarkAsSuperseded()      // Indexed → Superseded
```

**Invariantes:**
- Las transiciones de estado solo pueden seguir el flujo definido en ADR-009. Un `Document` no puede pasar de `Indexed` a `Processing` directamente; debe pasar por `Superseded` primero.
- `FileSizeBytes` no puede ser cero ni negativo.
- Solo se permiten los `FileType` definidos en el enum (`.pdf` y `.txt` para el MVP).

---

### 4.6 DocumentChunk
**Tipo:** Entity (pertenece al agregado Document)  
**Proyecto:** `Senda.Core.AIConcierge`

Representa un fragmento semántico de un documento, con su texto y su vector de embedding. Es la unidad de búsqueda en pgvector.

```csharp
public class DocumentChunk : ITenantEntity
{
    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public Guid TenantId { get; private set; }         // Desnormalizado para Global Query Filter y búsqueda vectorial eficiente
    public string Content { get; private set; }        // Texto del chunk
    public int ChunkIndex { get; private set; }        // Posición ordinal dentro del documento (0-based)
    public int TokenCount { get; private set; }        // Tokens reales del chunk (puede variar del tamaño nominal)
    public float[] Embedding { get; private set; }     // Vector de 1536 dimensiones (text-embedding-3-small)

    // Relaciones
    public Document Document { get; }
}
```

**Nota de implementación EF Core:**
La propiedad `Embedding` se mapea a la columna `vector(1536)` de pgvector usando la configuración Fluent API de `pgvector` para EF Core:
```csharp
entity.Property(e => e.Embedding)
    .HasColumnType("vector(1536)");
```

**Invariantes:**
- `Content` no puede ser nulo ni vacío.
- `Embedding` debe tener exactamente 1,536 dimensiones (correspondiente al modelo `text-embedding-3-small`).
- `ChunkIndex` debe ser único dentro del mismo `DocumentId`.

---

### 4.7 ChatChannel
**Tipo:** Entity  
**Proyecto:** `Senda.Core.AIConcierge`

Representa un canal de comunicación configurado por el tenant. Cada canal tiene su propia configuración (token de acceso, nombre de presentación, etc.) y puede ser habilitado o deshabilitado individualmente.

```csharp
public class ChatChannel : ITenantEntity, IAuditableEntity
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public ChannelType Type { get; private set; }          // Enum: Website, WhatsApp, Telegram
    public string DisplayName { get; private set; }        // Ej: "Chat del Sitio Web Principal"
    public string PublicApiKey { get; private set; }       // API Key pública de solo lectura para el widget
    public bool IsEnabled { get; private set; }

    // Configuración específica por tipo de canal (serializada como JSON en la DB)
    public ChannelConfiguration Configuration { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Relaciones
    public Tenant Tenant { get; }
    public IReadOnlyCollection<ChatSession> Sessions { get; }
}
```

**Value Objects:**

```csharp
public enum ChannelType
{
    Website,
    WhatsApp,
    Telegram
}

// Value Object: configuración polimórfica serializada como JSON
public abstract class ChannelConfiguration { }

public class WebsiteChannelConfiguration : ChannelConfiguration
{
    public string AllowedOrigins { get; init; }   // CORS: dominios permitidos para el widget
    public string PrimaryColor { get; init; }     // Color principal del widget (#HEX)
    public string WelcomeMessage { get; init; }   // Mensaje inicial que muestra el chat
}

public class WhatsAppChannelConfiguration : ChannelConfiguration
{
    public string PhoneNumberId { get; init; }    // ID del número en la API de WhatsApp Business
    public string AccessToken { get; init; }      // Token de la API de Meta (almacenado cifrado)
    public string WebhookVerifyToken { get; init; }
}

public class TelegramChannelConfiguration : ChannelConfiguration
{
    public string BotToken { get; init; }         // Token del bot de Telegram (almacenado cifrado)
    public string BotUsername { get; init; }
}
```

**Invariantes:**
- El `PublicApiKey` se genera automáticamente al crear el canal y no puede modificarse.
- Un tenant no puede tener dos canales activos del mismo `ChannelType`.
- Los tokens sensibles (`AccessToken`, `BotToken`) deben almacenarse cifrados en la base de datos.

---

### 4.8 ChatSession
**Tipo:** Aggregate Root  
**Proyecto:** `Senda.Core.AIConcierge`

Representa una conversación individual entre un usuario final y el concierge de IA. Tiene su propio ciclo de vida independiente del canal.

```csharp
public class ChatSession : ITenantEntity, IAuditableEntity
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ChatChannelId { get; private set; }
    public string ExternalSessionId { get; private set; } // ID generado por el cliente (widget, WhatsApp user ID)
    public SessionStatus Status { get; private set; }     // Enum: Active, Closed
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }

    // Relaciones
    public Tenant Tenant { get; }
    public ChatChannel Channel { get; }
    public IReadOnlyCollection<ChatMessage> Messages { get; }
}

public enum SessionStatus
{
    Active,
    Closed
}
```

**Invariantes:**
- Una `ChatSession` cerrada no puede recibir nuevos mensajes.
- `ExternalSessionId` debe ser único por `ChatChannelId`.

---

### 4.9 ChatMessage
**Tipo:** Entity (pertenece al agregado ChatSession)  
**Proyecto:** `Senda.Core.AIConcierge`

Representa un mensaje individual dentro de una sesión de chat. Almacena tanto los mensajes del usuario como las respuestas de la IA.

```csharp
public class ChatMessage : ITenantEntity
{
    public Guid Id { get; private set; }
    public Guid ChatSessionId { get; private set; }
    public Guid TenantId { get; private set; }             // Desnormalizado para Global Query Filter
    public MessageRole Role { get; private set; }          // Enum: User, Assistant
    public string Content { get; private set; }            // Texto del mensaje
    public int TokenCount { get; private set; }            // Tokens del mensaje (para control de costos)

    // Metadata de la respuesta de la IA (solo cuando Role = Assistant)
    public string? ModelUsed { get; private set; }         // Ej: "gpt-4o-mini"
    public int? PromptTokens { get; private set; }         // Tokens del prompt enviado al LLM
    public int? CompletionTokens { get; private set; }     // Tokens de la respuesta del LLM
    public Guid[]? RetrievedChunkIds { get; private set; } // IDs de los chunks usados en el RAG (para trazabilidad)

    public DateTime CreatedAt { get; private set; }

    // Relaciones
    public ChatSession Session { get; }
}

public enum MessageRole
{
    User,
    Assistant
}
```

**Invariantes:**
- `Content` no puede ser nulo ni vacío.
- Los campos de metadata de IA (`ModelUsed`, `PromptTokens`, `CompletionTokens`, `RetrievedChunkIds`) solo pueden tener valor cuando `Role = Assistant`.

---

## 5. Mapa de Relaciones

```
Tenant (1) ──────────────────── (N) User
   │                                  │
   │                            (N) RefreshToken
   │
   ├──── (1) KnowledgeConfiguration
   │
   ├──── (N) Document (1) ──── (N) DocumentChunk
   │
   ├──── (N) ChatChannel (1) ──── (N) ChatSession (1) ──── (N) ChatMessage
   │
   └── [TenantId en todas las entidades via ITenantEntity]
```

---

## 6. Tabla Resumen de Entidades

| Entidad | Tipo DDD | Aggregate Root | Multi-tenant | Tabla DB |
|---|---|---|---|---|
| `Tenant` | Aggregate Root | Sí (propio) | No | `tenants` |
| `User` | Entity | No | Sí | `users` |
| `RefreshToken` | Entity | No | Sí | `refresh_tokens` |
| `KnowledgeConfiguration` | Entity | No | Sí | `knowledge_configurations` |
| `Document` | Aggregate Root | Sí | Sí | `documents` |
| `DocumentChunk` | Entity | No | Sí | `document_chunks` |
| `ChatChannel` | Entity | No | Sí | `chat_channels` |
| `ChatSession` | Aggregate Root | Sí | Sí | `chat_sessions` |
| `ChatMessage` | Entity | No | Sí | `chat_messages` |

---

## 7. Decisiones de Diseño Registradas

| Decisión | Justificación |
|---|---|
| `TenantId` desnormalizado en `DocumentChunk` y `ChatMessage` | Permite que el Global Query Filter de EF Core aplique el filtro de aislamiento sin necesidad de JOINs adicionales en la búsqueda vectorial. |
| `ChatChannel` como entidad propia (no enum en `ChatSession`) | Permite configuración por canal (tokens, colores, webhooks) y habilita múltiples instancias del mismo tipo de canal por tenant en el futuro. |
| Tokens sensibles en `ChannelConfiguration` | Se almacenarán cifrados con AES-256 usando la `IDataProtectionProvider` de ASP.NET Core. La entidad expone el valor descifrado solo en memoria. |
| `RetrievedChunkIds` en `ChatMessage` | Habilita la trazabilidad del pipeline RAG: el administrador puede auditar qué fragmentos de conocimiento originaron cada respuesta de la IA. |
| Métodos de dominio en `Document` para las transiciones de estado | Evita que la lógica de transición de estados quede dispersa en los handlers. El agregado hace cumplir las transiciones válidas. |

---

## 8. Lo que este modelo NO incluye (Post-MVP)

Los siguientes elementos están fuera del alcance del MVP y no deben implementarse en la Fase 1:

- **Roles granulares de usuario** (`TenantViewer`, permisos por módulo).
- **Versionado de documentos** con rollback a versiones anteriores.
- **Métricas de uso por tenant** (tokens consumidos, costo estimado por sesión).
- **Configuración de embedding por tenant** (cada tenant con su propio modelo).
- **Entidades de Senda Loyalty y Senda Micro ERP** (Fases 2 y 3).
