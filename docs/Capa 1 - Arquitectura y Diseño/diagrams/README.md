# Diagramas de Arquitectura — Senda AI Concierge

Todos los diagramas están escritos en **Mermaid** y se renderizan automáticamente en GitHub. Para visualizarlos localmente, usa la extensión [Mermaid Preview](https://marketplace.visualstudio.com/items?itemName=bierner.markdown-mermaid) en VS Code.

---

## Índice

| Diagrama | Archivo | Audiencia | Propósito |
|---|---|---|---|
| C4 Nivel 1 — Contexto | [c4-architecture.md](./c4-architecture.md) | Cualquier persona | ¿Qué es Senda y con qué sistemas habla? |
| C4 Nivel 2 — Contenedores | [c4-architecture.md](./c4-architecture.md) | Desarrolladores | ¿De qué piezas está hecho Senda? |
| RAG Pipeline — Indexación | [rag-pipeline.md](./rag-pipeline.md) | Desarrolladores | ¿Cómo se procesa un documento subido? |
| RAG Pipeline — Recuperación | [rag-pipeline.md](./rag-pipeline.md) | Desarrolladores | ¿Cómo fluye una pregunta hasta la respuesta? |
| Secuencia — Chat Flow | [chat-sequence.md](./chat-sequence.md) | Implementadores | ¿Qué hace cada componente en cada request? |
| Secuencia — Indexación | [chat-sequence.md](./chat-sequence.md) | Implementadores | ¿Qué hace cada componente al indexar un doc? |

---

## Convención para nuevos diagramas

Al añadir un nuevo diagrama:
1. Usa Mermaid en un bloque de código con la sintaxis ` ```mermaid `.
2. Nómbralo descriptivamente en kebab-case y añádelo a este índice.
3. Documenta la **audiencia** y el **propósito** del diagrama en el archivo.
4. Si el diagrama representa un flujo que tiene un ADR asociado, enlázalo en las notas.
