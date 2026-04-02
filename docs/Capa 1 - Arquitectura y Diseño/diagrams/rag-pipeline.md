# Diagrama del Pipeline RAG — Senda AI Concierge

El pipeline RAG (Retrieval-Augmented Generation) es el corazón del módulo AI Concierge. Se divide en dos fases independientes: **Indexación** (cuando el administrador sube un documento) y **Recuperación** (cuando un cliente hace una pregunta).

---

## Fase 1 — Indexación (Write Path)

Se ejecuta de forma asíncrona en el `Background Worker` cada vez que un administrador sube un documento nuevo o actualizado.

```mermaid
flowchart TD
    A([👤 Administrador\nsube archivo]) --> B

    subgraph API ["🌐 Senda API — Request síncrono"]
        B[Validar archivo\nTipo: PDF o TXT\nTamaño máximo]
        B --> C[Guardar archivo\nen Storage]
        C --> D[Crear entidad Document\nStatus: Uploaded]
        D --> E[Encolar job\nen IBackgroundTaskQueue\nStatus → Pending]
        E --> F([✅ 202 Accepted\ndevuelto al cliente])
    end

    subgraph WORKER ["⚙️ Background Worker — Procesamiento asíncrono"]
        G[Desencolar job\nStatus → Processing]
        G --> H{¿Qué tipo\nde archivo?}
        H -- PDF --> I[PdfTextExtractor\nExtrae texto plano\nvia PdfPig]
        H -- TXT --> J[TxtTextExtractor\nLee contenido UTF-8]
        I --> K
        J --> K
        K[TextChunker\n512 tokens por chunk\n100 tokens de overlap]
        K --> L[Para cada chunk:\nLlamar a IEmbeddingService]
        L --> M{¿Proveedor\nconfigurado?}
        M -- OpenAI default --> N[text-embedding-3-small\nOpenAI API\nVector 1536 dims]
        M -- Ollama local --> O[nomic-embed-text\nOllama API\nVector 768 dims]
        N --> P
        O --> P
        P[Guardar DocumentChunk\nen PostgreSQL + pgvector\nContent + Embedding + TenantId]
        P --> Q{¿Más chunks\npor procesar?}
        Q -- Sí --> L
        Q -- No --> R[Actualizar Document\nStatus → Indexed\nChunkCount = N\nIndexedAt = now]
        R --> S([✅ Documento disponible\npara consultas RAG])
    end

    subgraph ERROR ["❌ Manejo de errores"]
        T[Error en cualquier paso] --> U[Actualizar Document\nStatus → Failed\nProcessingError = detalle]
        U --> V([🔔 Admin puede\nreintentar desde dashboard])
    end

    E -.->|"Worker consume\nla cola"| G
    L -.->|"Error de API\no IO"| T

    style API fill:#e8f4fd,stroke:#2196F3
    style WORKER fill:#e8f5e9,stroke:#4CAF50
    style ERROR fill:#fdecea,stroke:#f44336
```

---

## Fase 2 — Recuperación (Read Path)

Se ejecuta de forma síncrona en cada mensaje de un usuario final, a través del endpoint de chat.

```mermaid
flowchart TD
    A([💬 Cliente final\nenvía mensaje]) --> B

    subgraph AUTH ["🔐 Autenticación del Canal"]
        B[Recibir request\nPOST /api/v1/chat/message]
        B --> C[Validar PublicApiKey\ndel ChatChannel]
        C --> D[Resolver TenantId\ndesde ChatChannel]
        D --> E{¿Sesión existente\npor ExternalSessionId?}
        E -- No --> F[Crear ChatSession\nStatus: Active]
        E -- Sí --> G[Cargar ChatSession\nexistente]
        F --> H
        G --> H
    end

    subgraph RAG ["🧠 Pipeline RAG — Orquestado por Semantic Kernel"]
        H[Guardar ChatMessage\nRole: User\nen la sesión]
        H --> I[Generar embedding\nde la pregunta del usuario\nvia IEmbeddingService]
        I --> J[Búsqueda vectorial\nen PostgreSQL + pgvector\nWHERE tenant_id = X\nORDER BY embedding <=> queryVector\nLIMIT MaxContextChunks]
        J --> K[Recuperar N chunks\nmás relevantes\ndefault: 5 chunks]
        K --> L[Construir prompt\ncon Semantic Kernel]
        L --> M["Prompt final:\n[System Prompt del tenant]\n---\nCONTEXTO RECUPERADO:\n{chunk_1}\n{chunk_2}\n...{chunk_N}\n---\nHISTORIAL:\n{últimos M mensajes}\n---\nPREGUNTA: {mensaje usuario}"]
        M --> N{¿Proveedor LLM\nconfigurado?}
        N -- OpenAI default --> O[GPT-4o-mini\nOpenAI Chat Completion API]
        N -- Ollama local --> P[Llama 3 u otro\nOllama Chat Completion API]
        O --> Q
        P --> Q
        Q[Respuesta del LLM]
        Q --> R[Guardar ChatMessage\nRole: Assistant\nContent: respuesta\nModelUsed + Tokens\nRetrievedChunkIds]
    end

    subgraph RESPONSE ["📤 Respuesta al cliente"]
        R --> S[Mapear a DTO\nde respuesta]
        S --> T([✅ 200 OK\nRespuesta enviada al canal])
    end

    subgraph NOCONTEXT ["⚠️ Sin contexto relevante"]
        J -.->|"Similitud < umbral\n o sin documentos indexados"| U[Respuesta de fallback\ndefinida en KnowledgeConfiguration\nEj: 'No tengo información\nsobre ese tema.']
        U --> R
    end

    style AUTH fill:#fff3e0,stroke:#FF9800
    style RAG fill:#e8f4fd,stroke:#2196F3
    style RESPONSE fill:#e8f5e9,stroke:#4CAF50
    style NOCONTEXT fill:#fdecea,stroke:#f44336
```

---

## Control del Historial de Chat (Context Window Management)

Un detalle crítico del pipeline es cuántos mensajes anteriores se incluyen en el prompt para mantener el contexto de la conversación, sin exceder el límite de tokens del modelo.

```mermaid
flowchart LR
    A[ChatSession\nN mensajes totales] --> B{¿N > MaxHistoryMessages\nconfigurable?}
    B -- No --> C[Incluir todos los\nmensajes en el prompt]
    B -- Sí --> D[Incluir solo los\núltimos M mensajes\ndefault: 10]
    C --> E[Calcular tokens totales\nHistorial + Chunks + System Prompt]
    D --> E
    E --> F{¿Tokens totales\n> límite del modelo?}
    F -- No --> G([✅ Prompt listo\npara el LLM])
    F -- Sí --> H[Reducir historial\nhasta que el total\nesté dentro del límite]
    H --> G
```

---

## Resumen de Responsabilidades por Componente

| Componente | Responsabilidad en el Pipeline |
|---|---|
| `ITextExtractorService` | Extrae texto plano de PDF o TXT |
| `ITextChunkerService` | Divide el texto en chunks de 512 tokens con 100 de overlap |
| `IEmbeddingService` | Genera el vector de 1536 dims (OpenAI) o 768 dims (Ollama) |
| `IDocumentChunkRepository` | Persiste y consulta chunks en PostgreSQL + pgvector |
| `IChatOrchestrationService` | Coordina el flujo completo de recuperación vía Semantic Kernel |
| `IFileStorageService` | Lee y escribe archivos en el sistema de almacenamiento |
