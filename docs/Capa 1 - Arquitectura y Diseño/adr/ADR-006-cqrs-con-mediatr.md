# ADR-006: CQRS con MediatR vs. Application Services Simples

## Estado
**Aceptado** — 2025

## Contexto

La capa `Senda.Application` necesita un patrón para organizar la lógica de negocio (casos de uso). En proyectos .NET con Clean Architecture, los dos enfoques más comunes son los Application Services tradicionales y el patrón CQRS implementado con MediatR.

La decisión afecta directamente la legibilidad del código, la facilidad de añadir nuevas funcionalidades, y la posibilidad de que contribuidores externos entiendan y extiendan el proyecto.

## Opciones Consideradas

### Opción A: Application Services Simples
Clases de servicio con múltiples métodos que agrupan lógica relacionada. Ejemplo: `DocumentService` con métodos `UploadAsync`, `DeleteAsync`, `GetByIdAsync`.

**Pros:**
- Familiar para la mayoría de desarrolladores .NET.
- Menos abstracciones: código más directo de seguir para proyectos pequeños.
- Sin dependencias adicionales de NuGet.

**Contras:**
- Los servicios tienden a crecer (God Classes) a medida que se añaden casos de uso.
- Dificulta la aplicación de principios SOLID (Single Responsibility) a nivel de clase.
- Añadir comportamientos transversales (logging, validación, autorización) requiere AOP o modificación de cada método.
- No escala bien para el objetivo de open source, donde contribuidores añaden casos de uso independientes.

### Opción B: CQRS con MediatR (Seleccionada)
Cada caso de uso es una clase independiente: un `Command` o `Query` con su `Handler` correspondiente. MediatR actúa como bus de mediación entre los controladores y los handlers, sin acoplamiento directo.

**Pros:**
- Cada caso de uso es una clase con responsabilidad única y bien delimitada: fácil de leer, testear y modificar de forma independiente.
- Los `Pipeline Behaviors` de MediatR permiten añadir comportamientos transversales (validación con FluentValidation, logging, manejo de excepciones) en un solo lugar, sin modificar los handlers.
- Estructura predecible para contribuidores: cualquier nueva funcionalidad sigue el mismo patrón `Command/Query → Handler`.
- CQRS separa explícitamente las operaciones de lectura (Queries) de las de escritura (Commands), facilitando la futura optimización diferenciada de cada tipo.
- Ampliamente adoptado en proyectos .NET de Clean Architecture: abundante documentación y ejemplos.

**Contras:**
- Más clases por caso de uso (Command + Handler + Response DTO = mínimo 3 artefactos por feature).
- Curva de aprendizaje inicial para desarrolladores no familiarizados con el patrón.
- MediatR introduce una indirección que puede dificultar el debugging si no se conoce el patrón.

## Decisión

Se adopta **CQRS con MediatR** para toda la capa `Senda.Application`.

### Convenciones de nomenclatura:

```
Senda.Application.AIConcierge/
├── Documents/
│   ├── Commands/
│   │   ├── UploadDocument/
│   │   │   ├── UploadDocumentCommand.cs       # Record con los datos de entrada
│   │   │   ├── UploadDocumentCommandHandler.cs # Lógica del caso de uso
│   │   │   └── UploadDocumentCommandValidator.cs # FluentValidation
│   │   └── DeleteDocument/
│   │       ├── DeleteDocumentCommand.cs
│   │       └── DeleteDocumentCommandHandler.cs
│   └── Queries/
│       └── GetDocumentsByTenant/
│           ├── GetDocumentsByTenantQuery.cs
│           ├── GetDocumentsByTenantQueryHandler.cs
│           └── DocumentDto.cs                 # DTO de respuesta
└── Chat/
    └── Commands/
        └── SendMessage/
            ├── SendMessageCommand.cs
            └── SendMessageCommandHandler.cs
```

### Pipeline Behaviors registrados (en orden de ejecución):

1. **`LoggingBehavior<TRequest, TResponse>`** — Registra entrada, salida y duración de cada request.
2. **`ValidationBehavior<TRequest, TResponse>`** — Ejecuta todos los `IValidator<TRequest>` de FluentValidation. Si hay errores, lanza `ValidationException` antes de llegar al handler.
3. **`ExceptionHandlingBehavior<TRequest, TResponse>`** — Captura excepciones no manejadas y las convierte en respuestas de error estructuradas.

## Consecuencias

- **Positivas:** Casos de uso atómicos, testeables de forma unitaria sin levantar el stack completo. Behaviors transversales en un solo lugar. Estructura escalable para contribuidores.
- **A gestionar:** Se debe documentar la convención de carpetas en el `CONTRIBUTING.md` para que los contribuidores externos sigan el mismo patrón. La indirección de MediatR debe explicarse en la guía de arquitectura.
- **Regla de proyecto:** Los controladores en `Senda.API` solo deben llamar a `_mediator.Send(command)` y devolver el resultado mapeado. Ningún controlador debe contener lógica de negocio.
