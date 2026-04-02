# Especificación General del Proyecto: Senda AI Concierge
**Versión:** 1.0.0
**Licencia:** AGPL v3.0
**Estado:** En Desarrollo (MVP)

## 1. Resumen Ejecutivo
**Senda AI Concierge** es el primer módulo del ecosistema Senda. Es un orquestador de Inteligencia Artificial diseñado para pequeñas y medianas empresas (PyMEs) que automatiza la atención al cliente utilizando la documentación interna del negocio. Emplea la arquitectura **RAG (Retrieval-Augmented Generation)** para garantizar que la IA responda basándose *únicamente* en los datos reales de la empresa, evitando alucinaciones y protegiendo la marca.

## 2. Objetivos del Sistema 
* **Precisión de Dominio:** Permitir a las empresas subir sus propios manuales, catálogos y listas de precios (PDF, TXT) para entrenar el contexto de la IA.
* **Soporte Multi-Tenant:** Ejecutar múltiples instancias de negocio (Tenants) desde una sola base de datos y despliegue, asegurando el aislamiento estricto de los datos de cada cliente.
* **Flexibilidad de Infraestructura (Cloud/Local):** Soportar tanto APIs comerciales (OpenAI) como modelos de lenguaje locales (Ollama/Llama 3) para empresas con requerimientos estrictos de privacidad de datos.
* **Interfaz de Usuario Rápida:** Proveer un dashboard administrativo ligero para la gestión del conocimiento y visualización del historial de chats.
* **Configuración guíada de clientes de Chat:** Los clientes de chat (Website, Whatsapp, Telegram) deben ser configurables por el usuario desde el dashboard.

## 3. Casos de Uso Principales

### 3.1 Gestión del Conocimiento (Administrador)
* **Ingesta de Documentos:** El administrador sube un archivo (ej. `menu_precios.pdf`).
* **Procesamiento:** El sistema extrae el texto, lo divide en fragmentos semánticos (chunks) y genera vectores de incrustación (embeddings).
* **Configuración de Personalidad:** El administrador define el *System Prompt* (ej. "Eres el asistente amable de la Clínica Dental X. Responde de forma concisa").

### 3.2 Interacción Conversacional (Cliente Final)
* **Consulta:** Un cliente envía un mensaje a través del widget web, WhatsApp o Telegram (ej. "¿A qué hora abren los sábados?").
* **Recuperación:** El sistema convierte la pregunta en un vector, busca los fragmentos de conocimiento más relevantes en la base de datos y los inyecta en el prompt.
* **Respuesta:** El LLM formula una respuesta natural basada en los fragmentos recuperados.

## 4. Arquitectura y Flujo de Datos (RAG Pipeline)

El flujo central de Senda AI se divide en dos fases asíncronas:

| Fase | Proceso Interno | Tecnologías Involucradas |
| :--- | :--- | :--- |
| **1. Indexación (Write)** | Documento -> Extracción de Texto -> Chunker -> Embedding Model -> Guardado Vectorial | `ITextExtractorService`, Semantic Kernel (Text Embedding), PostgreSQL (`pgvector`) |
| **2. Recuperación (Read)** | Pregunta del Usuario -> Embedding -> Búsqueda de Similitud (Distancia L2/Coseno) -> Prompt Construction -> LLM Completion -> Respuesta | Semantic Kernel (Chat Completion), PostgreSQL (`pgvector`), .NET Web API |

## 5. Especificaciones Técnicas (Stack)

* **Backend / API:** .NET 10 (C#) bajo arquitectura *Modular Monolith* y principios de *Clean Architecture*.
* **Orquestación AI:** Microsoft Semantic Kernel.
* **Base de Datos:** PostgreSQL 16+ con la extensión `pgvector` habilitada.
* **Frontend (Admin UI):** Vue 3 (Composition API), Vite, PrimeVue y Tailwind CSS.
* **Despliegue:** Contenedores Docker (Docker Compose para el entorno orquestado).
* **Patrones de Diseño:** CQRS (mediante MediatR), Repository Pattern, Dependency Injection.

## 6. Limitaciones Técnicas y Guardrails (MVP)
* **Formatos Soportados:** Inicialmente, el extractor de texto solo soportará archivos `.txt` y `.pdf` (texto plano, sin OCR complejo de imágenes integradas).
* **Aislamiento de Vectores:** Todas las consultas a `pgvector` deben incluir de forma obligatoria el filtro `TenantId` a nivel de base de datos para prevenir cruce de información confidencial.
* **Límites de Tokens:** Se implementarán validaciones estrictas (FluentValidation) para limitar el tamaño del historial de chat enviado al LLM, optimizando costos de API y tiempos de respuesta.

## 7. Escalabilidad Futura (Post-MVP)
Una vez estabilizado el núcleo de Senda AI Concierge, el sistema está diseñado para:
1. Actuar como la base de identidad (Auth/Tenants) para los módulos futuros (*Senda Loyalty* y *Senda Micro ERP*).
2. Soportar "Function Calling" (Tools) en Semantic Kernel, permitiendo que la IA interactúe con el ERP para, por ejemplo, agendar una cita o verificar inventario en tiempo real.
