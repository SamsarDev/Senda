# ADR-007: Estrategia de Chunking de Documentos

## Estado
**Aceptado** — 2025

## Contexto

El pipeline de indexación RAG requiere dividir los documentos en fragmentos (chunks) antes de generar embeddings. La calidad del chunking impacta directamente la precisión de las respuestas del sistema: chunks demasiado pequeños pierden contexto; chunks demasiado grandes superan la ventana de contexto del LLM o diluyen la relevancia semántica.

El sistema debe soportar inicialmente archivos `.txt` y `.pdf` de texto plano. Los documentos típicos del segmento PyME incluyen: menús de precios, manuales de productos, políticas de servicio, FAQs, catálogos.

## Opciones Consideradas

### Opción A: Chunking Semántico
Agrupa oraciones con significado similar usando un modelo de lenguaje para detectar cambios de tema.

**Pros:**
- Máxima coherencia semántica en cada chunk.
- Chunks se alinean con los conceptos del documento.

**Contras:**
- Requiere un modelo adicional para la segmentación (complejidad y costo).
- Lentitud significativa en la indexación.
- Overhead innecesario para documentos estructurados simples (listas de precios, FAQs).
- Difícil de configurar y predecir para el usuario administrador.

### Opción B: Chunking Estructural (por párrafo o sección)
Divide respetando saltos de línea dobles, encabezados Markdown/HTML, o saltos de página en PDFs.

**Pros:**
- Respeta la estructura intencional del autor del documento.
- Sin modelo adicional requerido.

**Contras:**
- Los chunks resultantes tienen tamaño muy variable: un párrafo puede ser de 10 palabras o de 2,000.
- Chunks excesivamente largos pueden superar los límites del modelo de embedding.
- No funciona bien con documentos mal formateados o PDFs sin estructura clara (listas planas de precios, por ejemplo).

### Opción C: Fixed-Size con Overlap (Seleccionada)
Divide el texto en ventanas de N tokens con un solapamiento de M tokens entre chunks consecutivos. El overlap garantiza que el contexto en los límites de cada chunk no se pierda.

**Pros:**
- Implementación simple y predecible.
- Semantic Kernel incluye `TextChunker` con soporte nativo para este patrón.
- El tamaño y overlap son configurables por tenant en el futuro.
- Funciona correctamente independientemente de la calidad de formato del documento fuente.
- El overlap preserva el contexto en los límites entre chunks, mejorando la recuperación de información que cruza dos fragmentos.

**Contras:**
- Puede cortar una oración o idea a la mitad en el límite del chunk (mitigado parcialmente por el overlap).
- No respeta la estructura semántica del documento tan bien como las opciones A o B.

## Decisión

Se adopta la estrategia de **Fixed-Size Chunking con Overlap** para el MVP.

### Parámetros definidos:

| Parámetro | Valor | Justificación |
|---|---|---|
| `ChunkSize` | 512 tokens | Balanceo entre contexto suficiente y precisión de recuperación. Dentro del límite de la mayoría de modelos de embedding. |
| `ChunkOverlap` | 100 tokens | ~20% del tamaño del chunk. Suficiente para preservar contexto en límites sin duplicar información excesivamente. |
| Tokenizador | `cl100k_base` (tiktoken compatible) | El mismo tokenizador de `text-embedding-3-small` de OpenAI, garantizando que los límites de token sean consistentes entre el chunker y el modelo de embedding. |

### Implementación:

El servicio `ITextChunkerService` en `Senda.Core.AIConcierge` define el contrato. La implementación en `Senda.Infrastructure` utilizará `Microsoft.SemanticKernel.Text.TextChunker` con los parámetros definidos.

Cada `DocumentChunk` almacenado en la base de datos incluirá:
- `Content` (texto del chunk)
- `ChunkIndex` (posición ordinal dentro del documento)
- `TokenCount` (tokens reales del chunk)
- `Embedding` (vector generado)
- `TenantId` y `DocumentId` (para aislamiento y trazabilidad)

### Evolución planificada (post-MVP):

La interfaz `ITextChunkerService` está diseñada para ser intercambiable. En versiones futuras se pueden añadir implementaciones de chunking estructural o semántico sin modificar el pipeline de indexación.

## Consecuencias

- **Positivas:** Implementación simple con soporte nativo de Semantic Kernel. Parámetros configurables. Pipeline predecible y fácil de depurar.
- **A gestionar:** Si un documento tiene párrafos muy cortos (ej. un FAQ con respuestas de 2 líneas), varios chunks pueden contener información incompleta. Se debe evaluar si un mínimo de tamaño de chunk (`MinChunkSize`) es necesario para evitar chunks demasiado pequeños.
- **Pendiente de validación:** Los parámetros de 512/100 tokens deben validarse empíricamente durante las pruebas de integración con documentos reales del segmento PyME. Pueden ajustarse antes del primer release.
