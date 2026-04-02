# Diagrama de Secuencia — Flujo de Chat

Este diagrama describe el flujo completo a nivel de componentes para un mensaje de usuario final, desde que llega al endpoint de la API hasta que la respuesta es devuelta. Es la referencia principal para implementar los handlers de la capa `Senda.Application`.

---

## Flujo Principal — Mensaje de chat con contexto RAG

```mermaid
sequenceDiagram
    actor Client as Cliente Final<br/>(Widget / WhatsApp / Telegram)
    participant API as ChatController<br/>Senda.API
    participant Auth as ChannelAuthMiddleware
    participant MediatR as MediatR Bus
    participant Handler as SendMessageCommandHandler<br/>Senda.Application
    participant SessionRepo as IChatSessionRepository
    participant EmbSvc as IEmbeddingService
    participant ChunkRepo as IDocumentChunkRepository
    participant KnowledgeRepo as IKnowledgeConfigurationRepository
    participant SK as Semantic Kernel<br/>IChatOrchestrationService
    participant LLM as LLM Provider<br/>(OpenAI / Ollama)
    participant MsgRepo as IChatMessageRepository

    Client->>+API: POST /api/v1/chat/message<br/>{ publicApiKey, externalSessionId, content }

    API->>+Auth: ValidateChannelApiKeyAsync(publicApiKey)
    Auth-->>-API: ChatChannel { TenantId, ChannelId, IsEnabled }

    alt Canal deshabilitado
        API-->>Client: 403 Forbidden — Canal inactivo
    end

    API->>+MediatR: Send(SendMessageCommand { TenantId, ChannelId, ExternalSessionId, Content })

    MediatR->>+Handler: Handle(command)

    Note over Handler: Paso 1 — Resolver o crear sesión

    Handler->>+SessionRepo: GetByExternalIdAsync(ExternalSessionId, ChannelId)
    alt Sesión no existe
        SessionRepo-->>Handler: null
        Handler->>SessionRepo: CreateAsync(new ChatSession { ... })
    else Sesión existe
        SessionRepo-->>-Handler: ChatSession
    end

    alt Sesión cerrada
        Handler-->>MediatR: Error — Sesión cerrada
        MediatR-->>API: ValidationException
        API-->>Client: 400 Bad Request — Sesión cerrada
    end

    Note over Handler: Paso 2 — Persistir mensaje del usuario

    Handler->>+MsgRepo: CreateAsync(ChatMessage { Role: User, Content })
    MsgRepo-->>-Handler: ChatMessage guardado

    Note over Handler: Paso 3 — Generar embedding de la pregunta

    Handler->>+EmbSvc: GenerateEmbeddingAsync(command.Content)
    EmbSvc->>+LLM: POST /v1/embeddings<br/>{ input: content, model: text-embedding-3-small }
    LLM-->>-EmbSvc: float[1536]
    EmbSvc-->>-Handler: float[] queryEmbedding

    Note over Handler: Paso 4 — Búsqueda vectorial (RAG Retrieval)

    Handler->>+ChunkRepo: SearchSimilarAsync(queryEmbedding, TenantId, limit: 5)
    Note right of ChunkRepo: SELECT content, 1 - (embedding <=> $queryVec) AS score<br/>FROM document_chunks<br/>WHERE tenant_id = $tenantId<br/>ORDER BY embedding <=> $queryVec<br/>LIMIT 5
    ChunkRepo-->>-Handler: List<DocumentChunk> relevantChunks

    Note over Handler: Paso 5 — Cargar configuración del tenant

    Handler->>+KnowledgeRepo: GetByTenantAsync(TenantId)
    KnowledgeRepo-->>-Handler: KnowledgeConfiguration { SystemPrompt, LlmModel, Temperature, MaxResponseTokens }

    Note over Handler: Paso 6 — Cargar historial reciente de la sesión

    Handler->>+MsgRepo: GetRecentBySessionAsync(SessionId, limit: 10)
    MsgRepo-->>-Handler: List<ChatMessage> history

    Note over Handler: Paso 7 — Orquestar con Semantic Kernel

    Handler->>+SK: CompleteChatAsync(systemPrompt, relevantChunks, history, userMessage, config)

    Note right of SK: Construye el prompt:<br/>1. System Prompt (del tenant)<br/>2. Chunks recuperados como contexto<br/>3. Historial de mensajes recientes<br/>4. Mensaje actual del usuario

    SK->>+LLM: POST /v1/chat/completions<br/>{ model, messages, temperature, max_tokens }
    LLM-->>-SK: ChatCompletion { content, usage: { prompt_tokens, completion_tokens } }
    SK-->>-Handler: ChatCompletionResult { Content, PromptTokens, CompletionTokens }

    Note over Handler: Paso 8 — Persistir respuesta del asistente

    Handler->>+MsgRepo: CreateAsync(ChatMessage {<br/>  Role: Assistant,<br/>  Content: result.Content,<br/>  ModelUsed: config.LlmModel,<br/>  PromptTokens: result.PromptTokens,<br/>  CompletionTokens: result.CompletionTokens,<br/>  RetrievedChunkIds: relevantChunks.Select(c => c.Id)<br/>})
    MsgRepo-->>-Handler: ChatMessage guardado

    Handler-->>-MediatR: SendMessageResult { AssistantMessage, SessionId }
    MediatR-->>-API: SendMessageResult
    API-->>Client: 200 OK<br/>{ sessionId, message: { role: "assistant", content, createdAt } }
```

---

## Flujo Alternativo — Sin documentos indexados

Cuando el tenant no tiene documentos indexados o ningún chunk supera el umbral mínimo de similitud, el sistema responde con un mensaje de fallback en lugar de intentar fabricar una respuesta sin contexto.

```mermaid
sequenceDiagram
    participant Handler as SendMessageCommandHandler
    participant ChunkRepo as IDocumentChunkRepository
    participant KnowledgeRepo as IKnowledgeConfigurationRepository
    participant SK as Semantic Kernel
    participant LLM as LLM Provider

    Handler->>ChunkRepo: SearchSimilarAsync(queryEmbedding, TenantId, limit: 5)
    ChunkRepo-->>Handler: [] lista vacía (sin chunks o similitud < 0.75)

    Handler->>KnowledgeRepo: GetByTenantAsync(TenantId)
    KnowledgeRepo-->>Handler: KnowledgeConfiguration { FallbackMessage }

    Note over Handler: Si FallbackMessage está configurado,<br/>responder directamente sin llamar al LLM.<br/>Si no está configurado, llamar al LLM<br/>con el system prompt únicamente<br/>(sin contexto RAG).

    alt FallbackMessage configurado
        Handler->>Handler: Usar FallbackMessage como respuesta
        Note right of Handler: No se consume créditos de la API del LLM.<br/>Respuesta inmediata y económica.
    else Sin FallbackMessage
        Handler->>SK: CompleteChatAsync(systemPrompt, chunks=[], history, userMessage)
        SK->>LLM: POST /v1/chat/completions
        LLM-->>SK: Respuesta basada solo en el system prompt
        SK-->>Handler: ChatCompletionResult
    end
```

---

## Flujo de Indexación — Referencia rápida

```mermaid
sequenceDiagram
    actor Admin as Administrador
    participant API as DocumentsController
    participant MediatR as MediatR Bus
    participant Handler as UploadDocumentCommandHandler
    participant Storage as IFileStorageService
    participant DocRepo as IDocumentRepository
    participant Queue as IBackgroundTaskQueue
    participant Worker as IndexingBackgroundService
    participant Extractor as ITextExtractorService
    participant Chunker as ITextChunkerService
    participant EmbSvc as IEmbeddingService
    participant ChunkRepo as IDocumentChunkRepository

    Admin->>+API: POST /api/v1/documents<br/>multipart/form-data { file }
    API->>+MediatR: Send(UploadDocumentCommand { File, TenantId })
    MediatR->>+Handler: Handle(command)
    Handler->>+Storage: SaveAsync(file) → storagePath
    Storage-->>-Handler: storagePath
    Handler->>+DocRepo: CreateAsync(Document { Status: Uploaded })
    DocRepo-->>-Handler: Document { Id }
    Handler->>DocRepo: MarkAsPendingAsync(documentId)
    Handler->>+Queue: EnqueueAsync(IndexDocumentJob { DocumentId, TenantId })
    Queue-->>-Handler: OK
    Handler-->>-MediatR: UploadDocumentResult { DocumentId }
    MediatR-->>-API: Result
    API-->>Admin: 202 Accepted { documentId }

    Note over Worker: El worker consume la cola de forma asíncrona

    Worker->>+DocRepo: MarkAsProcessingAsync(documentId)
    Worker->>+Storage: ReadAsync(storagePath) → Stream
    Storage-->>-Worker: fileStream
    Worker->>+Extractor: ExtractTextAsync(fileStream, fileType) → string
    Extractor-->>-Worker: rawText
    Worker->>+Chunker: ChunkAsync(rawText) → List<TextChunk>
    Chunker-->>-Worker: chunks[N]

    loop Para cada chunk
        Worker->>+EmbSvc: GenerateEmbeddingAsync(chunk.Content)
        EmbSvc-->>-Worker: float[1536]
        Worker->>ChunkRepo: CreateAsync(DocumentChunk { Content, Embedding, TenantId })
    end

    Worker->>+DocRepo: MarkAsIndexedAsync(documentId, chunkCount: N)
    DocRepo-->>-Worker: OK
```

---

## Convenciones para la Implementación

Al implementar los handlers basándote en estos diagramas, se deben seguir las siguientes convenciones:

**Nomenclatura de interfaces:** Todas las interfaces del diagrama viven en `Senda.Core.AIConcierge`. Sus implementaciones concretas viven en `Senda.Infrastructure`.

**Manejo de errores:** Cualquier excepción lanzada dentro de un handler es capturada por el `ExceptionHandlingBehavior` de MediatR (definido en ADR-006), que la convierte en una respuesta de error estructurada con `ProblemDetails`.

**Cancelación:** Todos los métodos asincrónicos reciben un `CancellationToken` que debe propagarse hasta las llamadas a la API externa (OpenAI/Ollama) para evitar requests huérfanos si el cliente se desconecta.

**Logs:** El `LoggingBehavior` de MediatR registra automáticamente la entrada y salida de cada handler. Dentro de los handlers no se debe duplicar ese logging; solo se loguean eventos de negocio relevantes (ej. "Documento indexado con N chunks").
