# ADR-003: Semantic Kernel vs. Alternativas de Orquestación de IA

## Estado
**Aceptado** — 2025

## Contexto

El módulo AI Concierge requiere un orquestador de IA capaz de gestionar: generación de embeddings, construcción de prompts con contexto RAG, llamadas a modelos de completación de chat, e intercambio de proveedores de LLM (OpenAI ↔ Ollama). 

El ecosistema de herramientas de orquestación de IA es amplio. La decisión debe considerar la compatibilidad con el ecosistema .NET, dado que el resto del sistema está construido en C#.

## Opciones Consideradas

### Opción A: LangChain (Python) / LlamaIndex (Python)
Librerías Python ampliamente adoptadas en la industria de IA.

**Pros:**
- Ecosistema de Python más maduro para IA/ML.
- Mayor cantidad de ejemplos, tutoriales y modelos soportados.
- LlamaIndex tiene integración nativa muy completa para pipelines RAG.

**Contras:**
- Requiere introducir un servicio Python separado en el stack, añadiendo complejidad operativa.
- La comunicación entre el API .NET y el orquestador Python sería via HTTP o mensaje, añadiendo latencia y un punto de fallo adicional.
- Gestión de dos lenguajes, dos entornos virtuales, dos procesos. Incompatible con el objetivo de simplicidad operativa.

### Opción B: LangChain4j / SDK directo de OpenAI para .NET
Usar el SDK oficial de OpenAI para .NET (`Azure.AI.OpenAI` o `OpenAI`) sin orquestador.

**Pros:**
- Control total sobre cada llamada a la API.
- Sin abstracción adicional: código más directo.

**Contras:**
- Sin abstracción de proveedores: cambiar de OpenAI a Ollama requeriría reescribir la lógica de integración.
- La gestión del pipeline RAG (memory, context window, historial de chat) debe implementarse manualmente.
- Mayor superficie de código a mantener para funcionalidades que un orquestador provee out-of-the-box.

### Opción C: Microsoft Semantic Kernel (Seleccionada)
Framework de orquestación de IA de Microsoft, diseñado nativamente para .NET (con soporte también en Python y Java).

**Pros:**
- Integración nativa con el ecosistema .NET: inyección de dependencias, logging, configuración estándar de `appsettings.json`.
- Abstracción de proveedores mediante conectores (`IChatCompletionService`, `ITextEmbeddingGenerationService`): cambiar de OpenAI a Ollama es un cambio de configuración, no de código.
- Gestión de memoria de conversación y construcción de prompts integrada.
- Soporte para Function Calling / Plugins, habilitando la integración futura con el Micro ERP (ADR relacionado al roadmap).
- Soporte oficial y mantenimiento activo por parte de Microsoft.
- Un solo proceso, un solo lenguaje, sin servicios adicionales.

**Contras:**
- Framework relativamente joven; la API ha tenido cambios entre versiones menores.
- Menos ejemplos comunitarios que LangChain (aunque la documentación oficial es sólida).
- Abstracción puede ocultar el comportamiento exacto de las llamadas a la API en algunos casos.

## Decisión

Se adopta **Microsoft Semantic Kernel** como orquestador de IA del ecosistema Senda.

Se utilizará la versión estable más reciente disponible al inicio del desarrollo. Los servicios de Semantic Kernel se registrarán en el contenedor de DI de .NET (`IServiceCollection`) en `Senda.Infrastructure`, exponiendo únicamente las interfaces definidas en `Senda.Core.AIConcierge` (`IEmbeddingService`, `IChatService`). De esta forma, el resto de la aplicación desconoce que Semantic Kernel es la implementación subyacente.

## Consecuencias

- **Positivas:** Un único proceso y lenguaje. Intercambio de proveedores LLM via configuración. Capacidad de Function Calling disponible para la integración futura con el Micro ERP.
- **A gestionar:** Se debe fijar la versión de Semantic Kernel en el `Directory.Packages.props` y revisar el changelog ante actualizaciones, ya que el SDK ha introducido breaking changes entre versiones menores en el pasado.
- **Deuda técnica aceptada:** Si el proyecto requiere capacidades avanzadas de IA no soportadas por Semantic Kernel (ej. fine-tuning, pipelines multi-agente complejos), se evaluará introducir un servicio Python dedicado como componente opcional.
