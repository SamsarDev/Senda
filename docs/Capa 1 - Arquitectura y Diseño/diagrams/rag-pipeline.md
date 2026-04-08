# Diagrama del Pipeline RAG — Senda AI Concierge

El pipeline RAG (Retrieval-Augmented Generation) es el corazón del módulo AI Concierge. Se divide en dos fases independientes: **Indexación** (cuando el administrador sube un documento) y **Recuperación** (cuando un cliente hace una pregunta).

---

## Fase 1 — Indexación (Write Path)

Se ejecuta de forma asíncrona en el `Background Worker` cada vez que un administrador sube un documento nuevo o actualizado.

```mermaid
      subgraph API ["🌐 Senda API / Application — Request síncrono"]
        B[Validar archivo\nTipo: PDF, TXT, CSV\nTamaño máximo]
        B --> C[Guardar archivo\nen Storage]
        C --> D[Crear entidad KnowledgeDocument\nStatus: Processing]
        D --> H{¿Qué tipo\nde archivo?}
        H -- PDF --> I[TextExtractorService\nExtrae texto plano\nvia PdfPig]
        H -- TXT/CSV --> J[TextExtractorService\nLee contenido Stream]
        I --> K
        J --> K
        K[TextChunkerService\nEstrategia de palabras\ncon overlap]
        K --> L[Para cada chunk:\nLlamar a ITextEmbeddingService]
        L --> M[Ollama / OpenAI\nVector 1536 dims]
        M --> P[Guardar KnowledgeChunk\nen PostgreSQL + pgvector\nContent + Embedding + TenantId]
        P --> Q{¿Más chunks\npor procesar?}
        Q -- Sí --> L
        Q -- No --> R[Actualizar KnowledgeDocument\nStatus → Completed\nChunkCount = N\nProcessedAt = now]
        R --> S([✅ Procesamiento Finalizado\ndevuelto al cliente])
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
        H --> I[Generar embedding\nde la pregunta del usuario\nvia ITextEmbeddingService]
        I --> J[Búsqueda vectorial\nen PostgreSQL + pgvector\nWHERE tenant_id = X\nORDER BY L2Distance(queryVector)\nLIMIT MaxResults]
        J --> K[Recuperar N chunks\nmás relevantes]
        K --> L[Construir prompt\ncon Semantic Kernel]
        L --> M["Prompt final:\n[Contexto Recuperado]\n---\n[Historial reciente]\n---\n[Pregunta]"]
        M --> N[Ollama / OpenAI\nChat Completion Service]
        N --> Q
        Q[Respuesta del LLM]
        Q --> R[Guardar ChatMessage\nRole: Assistant\nContent: respuesta\nSourceContext: metadata]
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
| `ITextExtractorService` | Extrae texto plano de PDF, TXT o CSV |
| `ITextChunkerService` | Divide el texto en fragmentos con solapamiento |
| `ITextEmbeddingService` | Genera el vector de 1536 dims (Ollama/OpenAI) |
| `IVectorSearchRepository` | Consulta chunks similares en PostgreSQL + pgvector |
| `IChatCompletionService` | Coordina la respuesta RAG vía Semantic Kernel |
| `IFileStorageService` | Gestiona el almacenamiento físico (Azure/Local) |
| `IKnowledgeRepository` | Persiste metadatos de documentos y chunks |
