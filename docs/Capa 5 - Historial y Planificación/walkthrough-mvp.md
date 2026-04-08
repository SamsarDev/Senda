# Walkthrough: Senda AI Concierge MVP

Hemos completado la implementación de las Fases 1 a 4, logrando un MVP funcional que abarca desde la infraestructura de base de datos vectorial hasta la interfaz de usuario administrativa.

## 🚀 Logros Principales

### 1. Núcleo Multi-Tenant y Persistencia
- **Base de Datos**: Configurada con PostgreSQL y `pgvector` para almacenamiento semántico.
- **Aislamiento**: Implementado `TenantMiddleware` y filtros globales en EF Core para asegurar que cada empresa solo acceda a su información.

### 2. Pipeline RAG (Retrieval-Augmented Generation)
- **Extracción de Texto**: Soporte para PDF (vía PdfPig), TXT y CSV.
- **Semantic Kernel**: Integración con Ollama (Qwen3.5) para generación de respuestas y embeddings.
- **Busqueda Vectorial**: Repositorio especializado utilizando distancias L2 en PostgreSQL.

### 3. Frontend Administrativo (Senda Web)
- **Stack**: Vue 3 + PrimeVue 4 + Tailwind CSS.
- **Dashboard**: Interfaz moderna para gestionar documentos y empresas.
- **Chat Playground**: Área de pruebas para interactuar con la IA y visualizar el contexto recuperado.

### 4. Dockerización y Despliegue
- **Backend Docker**: Imagen optimizada basada en .NET 10.0.
- **Frontend Docker**: Servido vía Nginx para alta eficiencia.
- **Orquestación**: Listos para `docker-compose up -d`.

---

## 🛠️ Cómo Iniciar el Proyecto

### Requisitos Previos
- Docker / Podman (para la base de datos y Ollama).
- .NET 10 SDK.
- Node.js 18+.

### Paso 1: Levantar Infraestructura
```bash
docker-compose up -d
```

### Paso 2: Iniciar el Backend
```bash
dotnet run --project src/Senda.Api
```
El API estará disponible en `http://localhost:5231`.

### Paso 3: Iniciar el Frontend
```bash
cd src/Senda.Web
npm run dev
```
La interfaz estará disponible en `http://localhost:5173`.

---

## ✅ Verificación Final
- Compilación exitosa de la solución completa.
- Migraciones de base de datos validadas.
- Frontend construido y optimizado (`npm run build`).
