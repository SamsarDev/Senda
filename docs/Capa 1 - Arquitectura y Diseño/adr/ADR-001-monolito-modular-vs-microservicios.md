# ADR-001: Monolito Modular vs. Microservicios

## Estado
**Aceptado** — 2025

## Contexto

Senda es un ecosistema de tres módulos planificados (AI Concierge, Loyalty, Micro ERP) destinado a PyMEs y profesionales independientes. El equipo de desarrollo inicial es unipersonal, con perspectiva de abrir contribuciones en el futuro.

Se evaluó si la arquitectura inicial debería ser un sistema de microservicios independientes o un monolito con separación modular interna.

Los factores determinantes del contexto son:

- **Audiencia objetivo:** PyMEs con infraestructura limitada. Un despliegue de múltiples servicios independientes (con su propio networking, service discovery, etc.) representa una barrera operativa inaceptable.
- **Objetivo de despliegue mínimo:** El usuario final debe poder levantar el sistema completo con una API Key de OpenAI y un ambiente Docker, sin conocimientos de Kubernetes ni infraestructura compleja.
- **Fase actual:** Solo el Módulo 1 (AI Concierge) está en desarrollo activo. Los módulos 2 y 3 son planificación futura.
- **Equipo:** Un solo desarrollador en la fase inicial. Los microservicios introducen overhead operativo y de coordinación que no se justifica en este punto.

## Opciones Consideradas

### Opción A: Microservicios desde el inicio
Cada módulo (AI Concierge, Loyalty, Micro ERP) sería un servicio independiente con su propia base de datos, proceso y comunicación via mensajería o HTTP.

**Pros:**
- Escalabilidad y despliegue independiente por módulo.
- Tecnología heterogénea por servicio si fuera necesario.

**Contras:**
- Complejidad operativa alta: requiere service mesh, service discovery, o al mínimo un API Gateway con routing complejo.
- Overhead de desarrollo desproporcionado para un equipo unipersonal en fase MVP.
- Barrera de despliegue inaceptable para el usuario objetivo (PyME sin DevOps dedicado).
- Latencia de red añadida entre servicios que podrían compartir el mismo proceso.

### Opción B: Monolito Modular (Seleccionada)
Un único proceso de despliegue con módulos internamente bien delimitados, siguiendo los principios de Clean Architecture y Domain-Driven Design. Los módulos comparten el proceso y la base de datos, pero tienen límites de código claramente definidos.

**Pros:**
- Despliegue simple: un solo contenedor Docker Compose.
- Los módulos comparten la infraestructura de Auth y Multi-tenancy sin duplicación.
- Las fronteras de dominio están definidas en código (namespaces, proyectos separados), no en infraestructura.
- Migración futura a microservicios posible extrayendo módulos uno a uno si el volumen lo justifica.
- Curva de aprendizaje y operación adecuada para el usuario objetivo.

**Contras:**
- Un fallo crítico en un módulo afecta a todos (mitigado con manejo de excepciones robusto y circuit breakers).
- Escalado horizontal es del proceso completo, no por módulo (aceptable para el tamaño de PyME objetivo).

## Decisión

Se adopta la arquitectura de **Monolito Modular** para todo el ecosistema Senda.

La estructura de proyectos refleja esta decisión: `Senda.Core`, `Senda.Application` y sus sub-proyectos por módulo (`*.AIConcierge`, `*.Loyalty`, `*.MicroERP`) establecen los límites de dominio en el código. `Senda.Infrastructure` y `Senda.API` son únicos y compartidos.

Esta arquitectura no descarta una extracción futura a microservicios. Si en el futuro un módulo necesita escalar de forma independiente, los límites ya definidos facilitan esa extracción.

## Consecuencias

- **Positivas:** Despliegue simple con Docker Compose. Un solo pipeline de CI/CD. Auth y Multi-tenancy compartidos sin duplicación de lógica.
- **A gestionar:** Se deben respetar estrictamente los límites entre módulos. Un módulo no debe referenciar directamente las entidades de otro; la comunicación entre módulos debe ocurrir via interfaces definidas en `Senda.Core` o eventos de dominio.
- **Deuda técnica aceptada:** Si la carga de un módulo crece de forma desproporcionada, se deberá evaluar su extracción como servicio independiente.
