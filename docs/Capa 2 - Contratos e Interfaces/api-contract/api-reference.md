# API Reference — Senda AI Concierge v1

Resumen navegable del contrato de la API. La fuente de verdad completa está en [`openapi.yaml`](./openapi.yaml), importable en Swagger UI, Scalar o Postman.

| Mecanismo | Header | Usado en |
|---|---|---|
| **Tenant ID** | `X-Tenant-Id: <uuid>` | Todos los endpoints (MVP) |

### Base URL local
`http://localhost:5231/api`

### Obtener un token JWT

```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "email": "admin@clinicaperez.com",
  "password": "MiContraseña123!"
}
```

**Respuesta `200 OK`:**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiJ9...",
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2g...",
  "expiresAt": "2025-01-15T14:30:00Z",
  "user": {
    "id": "uuid",
    "email": "admin@clinicaperez.com",
    "firstName": "Carlos",
    "lastName": "Pérez",
    "role": "TenantAdmin"
  }
}
```

El `accessToken` expira en **15 minutos**. Usar `POST /auth/refresh` con el `refreshToken` para renovarlo sin re-autenticarse.

---

## Formato de Errores

Todos los errores siguen el estándar **RFC 7807 Problem Details**:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Validation Error",
  "status": 400,
  "errors": {
    "email": ["El campo 'email' no tiene un formato válido."]
  },
  "traceId": "00-abc123-00"
}
```

---

## Endpoints

### Auth — Autenticación

| Método | Endpoint | Auth | Descripción |
|---|---|---|---|
| `POST` | `/auth/login` | ❌ | Iniciar sesión. Devuelve JWT + refresh token. |
| `POST` | `/auth/refresh` | ❌ | Renovar access token con refresh token. |
| `POST` | `/auth/logout` | 🔐 JWT | Revocar refresh token activo. |

---

### Tenants — Gestión del negocio

| Método | Endpoint | Auth | Descripción |
|---|---|---|---|
| `POST` | `/Tenants` | ❌ | Registrar nuevo tenant. |
| `GET` | `/Tenants` | ❌ | Listar todos los tenants activos. |

#### Registro de nuevo tenant

```http
POST /api/Tenants?name=Clínica Dental Pérez
```

**Respuesta `200 OK`:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Clínica Dental Pérez",
  "isActive": true
}
```

---

### Knowledge — Base de conocimiento RAG

| Método | Endpoint | Auth | Descripción |
|---|---|---|---|
| `GET` | `/Knowledge/documents` | 🔑 Tenant | Listar documentos del tenant. |
| `POST` | `/Knowledge/upload` | 🔑 Tenant | Subir nuevo documento (PDF, TXT, CSV). |

#### Subir un documento

```http
POST /api/Knowledge/upload
X-Tenant-Id: <uuid>
Content-Type: multipart/form-data

file: <archivo.pdf>
```

**Respuesta `200 OK`:**
```json
{
  "documentId": "8a1b2c3d-4e5f-6789-abcd-ef0123456789"
}
```

El procesamiento es asíncrono. Consultar `GET /documents/{id}` para ver el estado:

```
Uploaded → Pending → Processing → Indexed
                         ↓
                       Failed  →  (retry)  →  Processing
```

#### Estados de un documento

| Estado | Descripción |
|---|---|
| `Uploaded` | Archivo recibido. Aún no encolado. |
| `Pending` | En cola para procesamiento. |
| `Processing` | Extracción, chunking y embedding en curso. |
| `Indexed` | Disponible para consultas RAG. `chunkCount` > 0. |
| `Failed` | Error durante el procesamiento. Ver `processingError`. |
| `Superseded` | Reemplazado por una versión más nueva. |

**Restricciones del MVP:**
- Tipos de archivo soportados: `.pdf`, `.txt` (solo texto plano, sin OCR de imágenes)
- Tamaño máximo: 10 MB por archivo
- No se puede eliminar un documento con estado `Processing`

---

### Knowledge — Configuración de la IA

| Método | Endpoint | Auth | Descripción |
|---|---|---|---|
| `GET` | `/knowledge/configuration` | 🔐 JWT | Ver system prompt y parámetros del LLM. |
| `PUT` | `/knowledge/configuration` | 🔐 JWT | Actualizar configuración de la IA. |

#### Actualizar la configuración

```http
PUT /api/v1/knowledge/configuration
Authorization: Bearer <token>
Content-Type: application/json

{
  "systemPrompt": "Eres el asistente amable de Clínica Dental Pérez. Responde de forma concisa y profesional. Solo responde preguntas relacionadas con los servicios de la clínica. Si no tienes información sobre algo, indica que te contacten directamente.",
  "llmModel": "gpt-4o-mini",
  "temperature": 0.3,
  "maxResponseTokens": 500,
  "maxContextChunks": 5
}
```

| Parámetro | Rango | Default | Descripción |
|---|---|---|---|
| `temperature` | 0.0 – 1.0 | 0.3 | Mayor valor = respuestas más creativas. Para RAG empresarial, valores bajos (0.2–0.4) dan más consistencia. |
| `maxResponseTokens` | 100 – 2000 | 500 | Límite de tokens en la respuesta del LLM. Impacta el costo de API. |
| `maxContextChunks` | 1 – 10 | 5 | Número de fragmentos del RAG a incluir en el prompt. Más chunks = más contexto pero mayor costo. |
| `llmModel` | ver enum | `gpt-4o-mini` | Modelo del LLM a usar. |

---

### Channels — Canales de comunicación

| Método | Endpoint | Auth | Descripción |
|---|---|---|---|
| `GET` | `/channels` | 🔐 JWT | Listar todos los canales configurados. |
| `POST` | `/channels` | 🔐 JWT | Crear nuevo canal (Website, WhatsApp o Telegram). |
| `GET` | `/channels/{id}` | 🔐 JWT | Ver configuración de un canal y su `publicApiKey`. |
| `PATCH` | `/channels/{id}` | 🔐 JWT | Actualizar configuración o habilitar/deshabilitar canal. |
| `DELETE` | `/channels/{id}` | 🔐 JWT | Eliminar canal y sus sesiones. |

#### Crear canal Website

```http
POST /api/v1/channels
Authorization: Bearer <token>
Content-Type: application/json

{
  "type": "Website",
  "displayName": "Chat del Sitio Web Principal",
  "configuration": {
    "channelType": "Website",
    "allowedOrigins": "https://www.clinicaperez.com",
    "primaryColor": "#1976D2",
    "welcomeMessage": "¡Hola! Soy el asistente virtual de Clínica Dental Pérez. ¿En qué puedo ayudarte?"
  }
}
```

**Respuesta `201 Created`:**
```json
{
  "id": "canal-uuid",
  "type": "Website",
  "displayName": "Chat del Sitio Web Principal",
  "publicApiKey": "pk_live_abc123def456...",
  "isEnabled": true,
  "configuration": { "..." },
  "createdAt": "2025-01-15T12:10:00Z"
}
```

> ⚠️ El `publicApiKey` se genera una sola vez al crear el canal y no puede modificarse. Guardarlo en un lugar seguro. Si se pierde, se debe eliminar y recrear el canal.

#### Crear canal WhatsApp

```json
{
  "type": "WhatsApp",
  "displayName": "WhatsApp Clínica",
  "configuration": {
    "channelType": "WhatsApp",
    "phoneNumberId": "123456789012345",
    "accessToken": "EAABwzLixnjYBO...",
    "webhookVerifyToken": "mi_token_secreto_webhook"
  }
}
```

#### Crear canal Telegram

```json
{
  "type": "Telegram",
  "displayName": "Bot de Telegram",
  "configuration": {
    "channelType": "Telegram",
    "botToken": "7123456789:AAHdqTcvCH1vGWJxfSeofSAs0K5PALDsaw",
    "botUsername": "ClinicaPerezBot"
  }
}
```

---

### Chat — Concierge de IA

| Método | Endpoint | Auth | Descripción |
|---|---|---|---|
| `POST` | `/Chat/send` | 🔑 Tenant | Enviar mensaje y recibir respuesta RAG. |

#### Enviar un mensaje

```http
POST /api/Chat/send
X-Tenant-Id: <uuid>
Content-Type: application/json

{
  "sessionId": null,
  "customerIdentifier": "user-123",
  "message": "¿Qué horarios tienen?"
}
```

**Respuesta `200 OK`:**
```json
{
  "sessionId": "session-uuid",
  "reply": "Atendemos de Lunes a Viernes de 9am a 6pm...",
  "sourceContext": "Horario de atención: Lunes a Viernes..."
}
```

**Flujo de sesión:**
- En el primer mensaje, omitir `externalSessionId` (o enviar uno nuevo). La respuesta incluirá el `sessionId`.
- En mensajes siguientes de la misma conversación, enviar el `externalSessionId` original para mantener el historial de contexto.
- Para WhatsApp y Telegram, usar el ID del usuario del canal como `externalSessionId` (ej. número de teléfono hash).

**Respuesta `503 Service Unavailable`:** Ocurre cuando el proveedor LLM (OpenAI u Ollama) no está disponible. El cliente debe implementar retry con backoff exponencial.

---

## Paginación

Los endpoints de listado usan paginación por offset:

```
GET /api/v1/documents?page=2&pageSize=20
```

**Estructura de respuesta paginada:**
```json
{
  "items": [...],
  "totalCount": 47,
  "page": 2,
  "pageSize": 20
}
```

---

## Guía de integración del widget web

Para integrar el concierge en un sitio web:

1. Crear un `ChatChannel` de tipo `Website` desde el dashboard.
2. Copiar el `publicApiKey` generado.
3. Llamar a `POST /api/v1/chat/message` con el `X-Api-Key` header desde el frontend.
4. Persistir el `sessionId` recibido en la primera respuesta (localStorage o sessionStorage).
5. Enviar el `sessionId` como `externalSessionId` en cada mensaje siguiente.

> El widget de referencia en Vue 3 está incluido en `Senda.Web`. Ver la documentación del dashboard para instrucciones de embebido.
