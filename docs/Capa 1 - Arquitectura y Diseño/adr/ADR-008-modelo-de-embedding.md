# ADR-008: Selección del Modelo de Embedding

## Estado
**Aceptado** — 2025

## Contexto

El pipeline RAG requiere convertir tanto los chunks de documentos (en la fase de indexación) como las preguntas de los usuarios (en la fase de recuperación) en vectores numéricos (embeddings) para realizar la búsqueda por similitud. La elección del modelo de embedding determina la calidad semántica de la recuperación, el costo operativo, la dimensionalidad del vector (y por lo tanto el almacenamiento en pgvector), y la posibilidad de operar en modo local (sin API externa).

El modelo seleccionado debe ser consistente entre indexación y recuperación: los embeddings de los chunks y los de las queries deben generarse con el mismo modelo para que la similitud sea significativa.

## Opciones Consideradas

### Opción A: `text-embedding-3-large` (OpenAI)
El modelo de embedding más potente de OpenAI, con vectores de 3,072 dimensiones.

**Pros:**
- Mayor capacidad de representación semántica.
- Mejor rendimiento en benchmarks de recuperación.

**Contras:**
- Costo 6.5x mayor que `text-embedding-3-small` ($0.13 vs $0.02 por millón de tokens).
- Vectores de 3,072 dimensiones: el doble de almacenamiento en pgvector que la opción small.
- La diferencia de calidad no justifica el costo adicional para el volumen de documentos esperado en PyMEs.

### Opción B: `nomic-embed-text` vía Ollama (Local)
Modelo de embedding open source ejecutable localmente con Ollama. Vectores de 768 dimensiones.

**Pros:**
- Costo cero de API. Privacidad total: ningún dato sale del servidor.
- Ideal para tenants con requerimientos estrictos de privacidad.

**Contras:**
- Requiere hardware con suficiente RAM/VRAM para ejecutar el modelo localmente.
- Calidad inferior a los modelos de OpenAI en español y textos de dominio empresarial.
- Añade complejidad operativa: el usuario debe instalar y configurar Ollama además de Docker.
- No adecuado como opción por defecto para el segmento PyME que no tiene infraestructura de IA local.

### Opción C: `text-embedding-3-small` (OpenAI) — Seleccionada como default
El modelo de embedding de OpenAI con mejor balance costo/calidad. Vectores de 1,536 dimensiones.

**Pros:**
- Excelente calidad semántica para texto en español e inglés en contextos de negocio.
- Costo operativo bajo: $0.02 por millón de tokens. Un catálogo de 100 páginas (~50,000 tokens) cuesta aproximadamente $0.001 en indexación.
- 1,536 dimensiones es el estándar de facto en implementaciones RAG empresariales: amplia bibliografía y compatibilidad con pgvector optimizado.
- Compatible con el requisito mínimo del proyecto (solo API Key de OpenAI).
- Soporte nativo en Semantic Kernel.

**Contras:**
- Requiere API Key de OpenAI (requisito asumido del proyecto).
- Los datos de los documentos se envían a OpenAI para generar el embedding (mitigado por los términos de uso de la API de OpenAI, que no usan los datos de API para entrenar modelos).

## Decisión

Se adopta **`text-embedding-3-small`** de OpenAI como modelo de embedding por defecto, con la arquitectura diseñada para soportar Ollama como alternativa configurable.

### Implementación definida:

La interfaz `IEmbeddingService` en `Senda.Core.AIConcierge` abstrae completamente el proveedor:

```csharp
public interface IEmbeddingService
{
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default);
}
```

En `Senda.Infrastructure`, se registran dos implementaciones:
- `OpenAIEmbeddingService` — Usa Semantic Kernel con el conector de OpenAI. **Default.**
- `OllamaEmbeddingService` — Usa Semantic Kernel con el conector de Ollama. Activado via configuración (`EmbeddingProvider: "Ollama"` en `appsettings.json`).

La columna `embedding` en la tabla `document_chunks` se define como `vector(1536)`. **Si en el futuro se cambia el modelo de embedding, se debe re-indexar toda la base de conocimiento**, ya que los vectores de distintos modelos no son comparables. Este proceso se documentará en la guía de operaciones.

### Dimensiones y almacenamiento estimado:

| Modelo | Dimensiones | Bytes por vector | Chunks estimados (PyME típica) | Almacenamiento total |
|---|---|---|---|---|
| `text-embedding-3-small` | 1,536 | ~6 KB | 5,000 | ~30 MB |
| `text-embedding-3-large` | 3,072 | ~12 KB | 5,000 | ~60 MB |
| `nomic-embed-text` | 768 | ~3 KB | 5,000 | ~15 MB |

El almacenamiento estimado para una PyME típica es negligible para cualquier proveedor de hosting moderno.

## Consecuencias

- **Positivas:** Excelente balance costo/calidad. Abstracción que permite cambiar a Ollama sin modificar el pipeline RAG. Compatible con el requisito de solo API Key de OpenAI.
- **A gestionar:** Cambiar el modelo de embedding en producción requiere re-indexar todos los documentos de todos los tenants. Se debe implementar un comando de re-indexación masiva y documentarlo claramente.
- **Post-MVP:** Evaluar la posibilidad de configurar el modelo de embedding a nivel de tenant, permitiendo que tenants con requerimientos de privacidad usen Ollama mientras otros usan OpenAI.
