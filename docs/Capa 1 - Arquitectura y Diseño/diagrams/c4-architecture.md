# Diagramas de Arquitectura C4 — Senda AI Concierge

Estos diagramas siguen el modelo **C4** (Context, Container, Component, Code) para describir la arquitectura del sistema en niveles progresivos de detalle.

---

## Nivel 1 — Contexto del Sistema

Muestra qué es Senda, quién lo usa y con qué sistemas externos se comunica. Es el punto de entrada para cualquier persona que llega al repositorio por primera vez.

```mermaid
C4Context
    title Sistema Senda AI Concierge — Contexto

    Person(admin, "Administrador del Negocio", "Gestiona el conocimiento, configura la IA y revisa el historial de conversaciones.")
    Person(endUser, "Cliente Final", "Interactúa con el concierge de IA a través de un canal (web, WhatsApp, Telegram).")

    System(senda, "Senda AI Concierge", "Orquestador de IA para PyMEs. Permite atención al cliente automatizada basada en la documentación real del negocio.")

    System_Ext(openai, "OpenAI API", "Proveedor de modelos de lenguaje (GPT-4o-mini) y generación de embeddings (text-embedding-3-small).")
    System_Ext(ollama, "Ollama (Local)", "Alternativa local para modelos LLM y embeddings. Para tenants con requerimientos estrictos de privacidad.")
    System_Ext(whatsapp, "WhatsApp Business API", "Canal de mensajería de Meta para atención al cliente vía WhatsApp.")
    System_Ext(telegram, "Telegram Bot API", "Canal de mensajería de Telegram.")

    Rel(admin, senda, "Sube documentos, configura system prompt, revisa chats", "HTTPS / Dashboard Vue 3")
    Rel(endUser, senda, "Envía preguntas y recibe respuestas", "Widget Web / WhatsApp / Telegram")
    Rel(senda, openai, "Genera embeddings y completaciones de chat", "HTTPS / REST API")
    Rel(senda, ollama, "Alternativa local para embeddings y LLM", "HTTP / REST API")
    Rel(senda, whatsapp, "Envía y recibe mensajes", "HTTPS / Webhooks")
    Rel(senda, telegram, "Envía y recibe mensajes", "HTTPS / Webhooks")

    UpdateLayoutConfig($c4ShapeInRow="3", $c4BoundaryInRow="1")
```

---

## Nivel 2 — Contenedores

Muestra las piezas tecnológicas que componen Senda: qué procesos corren, qué tecnologías usan y cómo se comunican entre sí.

```mermaid
C4Container
    title Sistema Senda AI Concierge — Contenedores

    Person(admin, "Administrador", "Gestiona documentos y configuración.")
    Person(endUser, "Cliente Final", "Hace preguntas al concierge.")

    System_Ext(openai, "OpenAI API", "LLM + Embeddings")
    System_Ext(ollama, "Ollama", "LLM + Embeddings (local)")
    System_Ext(whatsapp, "WhatsApp Business API", "Mensajería")
    System_Ext(telegram, "Telegram Bot API", "Mensajería")

    System_Boundary(senda, "Senda AI Concierge") {

        Container(webApp, "Dashboard Administrativo", "Vue 3, Vite, PrimeVue, Tailwind CSS", "Interfaz de usuario para gestión de documentos, configuración de la IA y visualización del historial de chats.")

        Container(api, "Senda API", ".NET 10, ASP.NET Core, Semantic Kernel", "API REST que orquesta toda la lógica de negocio: autenticación, ingesta de documentos, pipeline RAG y gestión de chats.")

        Container(worker, "Background Worker", ".NET 10, IHostedService", "Procesa la cola de indexación de documentos de forma asíncrona: extracción de texto, chunking y generación de embeddings.")

        ContainerDb(db, "Base de Datos", "PostgreSQL 16 + pgvector", "Almacena datos relacionales (tenants, usuarios, sesiones) y vectores de embeddings para la búsqueda semántica.")

        Container(storage, "Almacenamiento de Archivos", "Sistema de archivos local / Volumen Docker", "Guarda los archivos originales subidos por los administradores (PDF, TXT).")
    }

    Rel(admin, webApp, "Usa", "HTTPS")
    Rel(endUser, api, "Envía mensajes via widget web", "HTTPS / REST")
    Rel(whatsapp, api, "Webhook entrante", "HTTPS / POST")
    Rel(telegram, api, "Webhook entrante", "HTTPS / POST")

    Rel(webApp, api, "Llamadas a la API REST", "HTTPS / JSON")
    Rel(api, worker, "Encola documentos para indexar", "En memoria / IBackgroundTaskQueue")
    Rel(api, db, "Lee y escribe datos", "TCP / EF Core")
    Rel(worker, db, "Escribe chunks y embeddings", "TCP / EF Core")
    Rel(worker, storage, "Lee archivos subidos", "I/O local")
    Rel(api, storage, "Escribe archivos subidos", "I/O local")
    Rel(api, openai, "Genera embeddings y completaciones (default)", "HTTPS")
    Rel(worker, openai, "Genera embeddings durante indexación (default)", "HTTPS")
    Rel(api, ollama, "Alternativa local configurada por tenant", "HTTP")
    Rel(worker, ollama, "Alternativa local configurada por tenant", "HTTP")
    Rel(api, whatsapp, "Envía respuestas salientes", "HTTPS")
    Rel(api, telegram, "Envía respuestas salientes", "HTTPS")

    UpdateLayoutConfig($c4ShapeInRow="3", $c4BoundaryInRow="1")
```

---

## Notas de Arquitectura

### Sobre el Background Worker
El `Background Worker` es un `IHostedService` de .NET que corre dentro del mismo proceso que la `Senda API`. Esto evita la necesidad de un servicio separado (como un worker de Kubernetes o una función serverless) en el despliegue inicial. La comunicación es vía una cola en memoria (`IBackgroundTaskQueue`), implementada con `Channel<T>` de .NET.

**Implicación:** Si la API se reinicia durante el procesamiento de un documento, los jobs en memoria se pierden. Para el MVP esto se maneja con el estado `Processing` → detección al arranque → re-encolado automático. Post-MVP se puede migrar a una cola persistente (Redis Streams, RabbitMQ).

### Sobre el Almacenamiento de Archivos
Para el MVP, los archivos se almacenan en el sistema de archivos local del contenedor, montado como un volumen Docker. Esto es suficiente para un despliegue en VPS. La interfaz `IFileStorageService` abstrae la implementación, permitiendo migrar a S3/Azure Blob Storage sin cambiar la lógica de negocio.

### Sobre los Canales de Mensajería
WhatsApp y Telegram se integran vía webhooks: los proveedores envían mensajes entrantes como HTTP POST a endpoints de Senda. Senda procesa el mensaje, construye la respuesta vía RAG y llama a la API del proveedor para enviarla. El endpoint del webhook se configura por `ChatChannel`.
