# Guía de Inicio Rápido — Senda AI Concierge

Esta guía te ayudará a poner en marcha el entorno de desarrollo para el módulo **AI Concierge**.

## 1. Requisitos Previos

- **.NET 10 SDK**
- **Node.js 18+** y npm
- **Docker / Podman** (para base de datos y Ollama)
- **PostgreSQL** con la extensión `pgvector`

## 2. Configuración de Infraestructura

Levanta los contenedores necesarios usando Docker Compose:

```bash
docker-compose up -d
```

Esto iniciará:
- **PostgreSQL**: Puerto 5432 (Base de datos: `senda_db`)
- **Ollama**: Puerto 11434 (Servicio de IA local)

### Configurar Ollama
Descarga los modelos necesarios:
```bash
docker exec -it ollama ollama pull qwen2.5:7b
docker exec -it ollama ollama pull nomic-embed-text
```

## 3. Configuración del Backend

1. Navega a la raíz del proyecto.
2. Aplica las migraciones de base de datos:
```bash
dotnet ef database update --project src/Senda.Infrastructure --startup-project src/Senda.Api
```
3. Ejecuta la aplicación:
```bash
dotnet run --project src/Senda.Api
```
El API estará disponible en `http://localhost:5231`.

## 4. Configuración del Frontend

1. Navega al directorio del frontend:
```bash
cd src/Senda.Web
```
2. Instala las dependencias:
```bash
npm install
```
3. Inicia el servidor de desarrollo:
```bash
npm run dev
```
La aplicación estará disponible en `http://localhost:5173`.

## 5. Primeros Pasos en la UI

1. Abre `http://localhost:5173`.
2. Crea tu primera empresa (Tenant) desde el botón "Nueva Empresa".
3. Selecciona la empresa en el selector superior.
4. Sube un archivo PDF o TXT en la sección de "Gestión de Conocimiento".
5. Ve a la pestaña "Chat AI" y comienza a interactuar con tu documento.

---

## ⚙️ Configuración Avanzada (`appsettings.json`)

El archivo `src/Senda.Api/appsettings.json` contiene configuraciones clave:

- **AI:Provider**: `Ollama` | `OpenAI`.
- **AI:Ollama:Endpoint**: URL del servicio local.
- **Storage:AzureBlob**: Si se provee `ConnectionString`, se usará Azure. De lo contrario, se usará almacenamiento local en `wwwroot/storage`.
