# Senda: Enterprise-Grade AI Ecosystem for SMBs

Senda es un ecosistema modular de código abierto diseñado para empoderar a pequeñas y medianas empresas (PyMEs) con herramientas de nivel corporativo. A diferencia de las soluciones SaaS cerradas, Senda prioriza la **soberanía de datos**, la **escalabilidad modular** y la **inteligencia propietaria**.

> "El camino (Senda) hacia la transformación digital no debería ser complejo ni costoso, sino sólido y evolutivo."

---

## Stack Tecnológico

Seleccionado para ofrecer el máximo rendimiento con el menor costo operativo (TCO):

| Componente | Stack de Producción | Justificación Empresarial |
| :--- | :--- | :--- |
| API Gateway | .NET 10 Web API | Alto rendimiento, tipado fuerte y middleware de seguridad robusto. |
| Auth & Multi-tenancy | ASP.NET Core Identity + JWT | Permite gestionar múltiples pequeñas empresas en una sola instancia. |
| Vector Engine | PostgreSQL + pgvector | Evita la complejidad de gestionar una DB vectorial separada; confiabilidad probada. |
| Orquestador | Semantic Kernel | Estándar de Microsoft para pipelines de IA, facilita el intercambio de modelos (LLMs). |
| Admin UI | Vue 3 + PrimeVue + Tailwind | Interfaz rápida, limpia y profesional (estilo Dashboard administrativo). |
| Infrastructure | Docker Compose / Helm | Facilita el despliegue en cualquier VPS o Cloud (AWS/Azure). |

---

## Módulos

Actualmente **Senda** se encuentra en desarrollo y ha sido planificado como un ecosistema completo que ofrece 3 soluciones primordiales para la transformación digital de cualquier empresa. Cada solución ha sido planificada como 1 fase independiente del desarrollo del ecosistema.

### Roadmap del Ecosistema

Senda está diseñado para crecer por capas junto con el negocio:

- **[Fase 1] Senda AI Concierge:** Concierge inteligente y gestión de conocimiento.
- **[Fase 2] Senda Loyalty:** Sistema de fidelización QR y retención de clientes.
- **[Fase 3] Senda Micro ERP:** Gestión administrativa, cotizaciones y facturación localizada (GTM).

### Senda AI Concierge (Fase Actual)

El primer componente del ecosistema Senda es un orquestador de IA especializado en la atención al cliente y la gestión del conocimiento.

#### ¿Por qué Senda AI Concierge es diferente?

1. **RAG Nativo (Retrieval-Augmented Generation):** No es un chat genérico. Senda utiliza la documentación real de la empresa (PDFs, manuales, catálogos) para responder con precisión.
2. **Privacidad "Cloud-Hybrid":** Diseñado para funcionar con proveedores de LLM líderes (OpenAI/Azure) o en entornos locales (Ollama/Llama 3) para máxima privacidad.
3. **Arquitectura .NET Profesional:** Construido sobre Clean Architecture y Domain-Driven Design (DDD), garantizando un código mantenible y listo para auditorías de seguridad.
4. **Multi-tenancy Ready:** Estructura preparada para gestionar múltiples unidades de negocio o clientes desde una única instancia.

---

## Arquitectura

### Definición de estructura de la solución .NET

La solución está configurada como un **Monolito Modular** siguiendo los principios de _Clean Architecture_ y _Domain Driven Design_. Esto permite que la lógica de IA esté aislada de la infraestructura (DB, APIs externas).

### Estructura de carpetas definida

- **Senda.Core**: Entidades de dominio (Negocio, Documento, Sesión de Chat) e interfaces. No tiene dependencias externas.
    - **Senda.Core.AIConcierge**: Encapsulación de entidades e interfaces propias del módulo **Senda AI Concierge**.
    - **Senda.Core.Loyalty**: Encapsulación de entidades e interfaces propias del módulo **Senda Loyalty**.
    - **Senda.Core.MicroERP**: Encapsulación de entidades e interfaces propias del módulo **Senda.Core.MicroERP**.
- **Senda.Application**: Casos de uso y lógica de la aplicación.
    - **Senda.Core.AIConcierge**: Encapsulación de casos de uso propios del módulo **Senda AI Concierge**. Aquí es donde vive la lógica de orquestación con Semantic Kernel.
    - **Senda.Core.Loyalty**: Encapsulación de casos de uso propios del módulo **Senda Loyalty**.
    - **Senda.Core.MicroERP**: Encapsulación de casos de uso propios del módulo **Senda.Core.MicroERP**.
- **Senda.Infrastructure**: Implementaciones concretas e inyección de dependencias. Conexión a PostgreSQL (EF Core), cliente de OpenAI/Ollama, almacenamiento de archivos.
- **Senda.API**: Controladores, Auth, y configuración de la aplicación.
- **Senda.Web**: Proyecto de Vue 3 utilizado para el dashboard administrativo.