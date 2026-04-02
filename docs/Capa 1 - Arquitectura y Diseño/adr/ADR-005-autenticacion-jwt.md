# ADR-005: Estrategia de Autenticación — JWT con ASP.NET Core Identity

## Estado
**Aceptado** — 2025

## Contexto

El sistema requiere autenticación para dos tipos de actores distintos con necesidades diferentes:

- **Administrador del Tenant:** Accede al dashboard de Vue 3 para gestionar documentos, configurar el sistema prompt y revisar el historial de chats. Requiere sesiones con expiración y renovación.
- **Widget de Chat / Integraciones (WhatsApp, Telegram):** Accede al endpoint de chat de forma programática. Puede ser un acceso anónimo con contexto de sesión, o autenticado via API Key por canal.

La solución de auth debe integrarse con la estrategia de multi-tenancy definida en ADR-004, siendo el JWT el portador del `TenantId`.

## Opciones Consideradas

### Opción A: OAuth2 / OpenID Connect con proveedor externo (Auth0, Azure AD B2C)
Delegar la autenticación completamente a un proveedor externo de identidad.

**Pros:**
- Sin responsabilidad de gestionar passwords, tokens de reset, MFA.
- Estándares de seguridad maduros gestionados por terceros.

**Contras:**
- Dependencia externa crítica: si el proveedor tiene downtime, el sistema completo queda inaccesible.
- Costo mensual por usuarios activos (Auth0 tiene free tier limitado; Azure AD B2C tiene costo por MAU).
- Incompatible con el objetivo del proyecto: que una PyME pueda operar con solo una API Key de OpenAI, sin dependencias de servicios adicionales de pago.
- Complejidad de configuración que dificulta el onboarding de nuevos contribuidores del proyecto open source.

### Opción B: ASP.NET Core Identity + JWT (Seleccionada)
Usar ASP.NET Core Identity para la gestión de usuarios y contraseñas, emitiendo JWT firmados localmente en cada login exitoso.

**Pros:**
- Sin dependencias externas de pago: todo el ciclo de vida de autenticación ocurre dentro del sistema.
- ASP.NET Core Identity es una solución probada, mantenida por Microsoft, con manejo seguro de passwords (hashing con PBKDF2 por defecto).
- El JWT puede incluir claims customizados (`tenant_id`, `role`) que alimentan directamente el `ITenantContext` del ADR-004.
- Compatible con cualquier entorno de despliegue sin configuración adicional.
- Familiaridad en el ecosistema .NET: ampliamente documentado y con ejemplos abundantes.

**Contras:**
- La responsabilidad del almacenamiento seguro de credenciales recae en el sistema (mitigado por Identity, que gestiona el hashing correctamente).
- Sin MFA out-of-the-box (se puede añadir con Identity, pero requiere implementación adicional — diferido al post-MVP).
- La revocación de tokens requiere implementar una blacklist o usar Refresh Tokens con rotación (incluido en la implementación).

### Opción C: API Keys estáticas por tenant
Emitir una API Key por tenant que se envía en el header `X-Api-Key`.

**Pros:**
- Extremadamente simple para integraciones máquina-a-máquina.

**Contras:**
- Sin expiración ni rotación automática por defecto.
- No adecuado como mecanismo principal de auth del dashboard administrativo.
- Problemático para múltiples usuarios dentro del mismo tenant (todos comparten la misma key).

## Decisión

Se adopta **ASP.NET Core Identity + JWT** como mecanismo principal de autenticación.

### Implementación definida:

**Flujo de autenticación de administrador:**
1. `POST /api/v1/auth/login` recibe `{email, password}`.
2. Identity valida credenciales. Si son correctas, se emiten dos tokens:
   - **Access Token (JWT):** Expira en 15 minutos. Contiene claims: `sub` (UserId), `tenant_id`, `role`.
   - **Refresh Token:** Opaco, almacenado en DB (tabla `refresh_tokens`), expira en 7 días. Permite renovar el Access Token sin re-autenticarse.
3. El `ITenantContext` resuelve el `tenant_id` desde el claim del JWT en cada request.

**Claims del JWT:**
```json
{
  "sub": "user-uuid",
  "tenant_id": "tenant-uuid",
  "role": "TenantAdmin",
  "exp": 1234567890
}
```

**Acceso del widget de chat:**
Para el MVP, el endpoint de chat (`POST /api/v1/chat/message`) acepta requests sin autenticación de usuario final, identificando la sesión por un `session_id` generado en el cliente. El `tenant_id` se resuelve desde la configuración del widget (API Key pública del tenant, de solo lectura). Esta API Key pública es diferente de las credenciales del administrador.

**Firmado del JWT:**
La clave de firmado se configura via variable de entorno (`JWT__Secret`), con un mínimo de 256 bits. En producción, debe rotarse periódicamente.

## Consecuencias

- **Positivas:** Sin dependencias externas de pago. JWT transporta el `TenantId` al `ITenantContext`. Flujo de refresh tokens implementado desde el inicio.
- **A gestionar:** La clave JWT (`JWT__Secret`) debe tratarse como secreto crítico: nunca en el repositorio, siempre en variables de entorno o un gestor de secretos. Se debe documentar en el Getting Started.
- **Post-MVP:** Evaluar la adición de MFA via Identity (TOTP authenticator) para cuentas de administrador.
