# Savia Up Backend

Backend inicial de Savia Up, una plataforma SaaS multi-tenant para restaurantes y gastrobares. Está implementado en .NET 10, ASP.NET Core Controllers, Entity Framework Core 10 y PostgreSQL, con arquitectura hexagonal (Ports & Adapters).

## Proyectos y dependencias

```text
saviaup.backend.Shared          constantes y catálogo es/en
        ↑
saviaup.backend.Domain          entidades, DTO, Result y puertos
        ↑
saviaup.backend.Core            casos de uso y reglas de negocio
        ↑
saviaup.backend.Api             adapter HTTP (controllers y middleware)

saviaup.backend.Infrastructure  adapters de PostgreSQL, JWT, hashing, email y tiempo
        └────────────────────── implementa puertos de Domain
```

`Api` compone `DomainModule`, `CoreModule`, `InfrastructureModule` y `ApiModule`. Core no conoce EF, Npgsql, SMTP ni `HttpContext`; Infrastructure no contiene reglas del flujo HTTP.

## Requisitos

- .NET SDK 10.0.302 o compatible.
- PostgreSQL 17 o una versión moderna soportada por Npgsql 10.
- Docker Desktop, opcional.

## Configuración local

Nunca guardes una cadena PostgreSQL, clave JWT o credencial SMTP en Git. Para desarrollo local usa .NET User Secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:SaviaUp" "Host=localhost;Port=5432;Database=saviaup;Username=usuario;Password=clave" --project saviaup.backend.Api
dotnet user-secrets set "Jwt:SigningKey" "una-clave-local-aleatoria-de-al-menos-32-bytes" --project saviaup.backend.Api
dotnet user-secrets list --project saviaup.backend.Api
```

API e Infrastructure comparten el mismo `UserSecretsId`. El host usa ese almacén al ejecutar en Development y `SaviaUpDbContextFactory` lo usa directamente durante `dotnet ef`, de modo que las migraciones no dependen de una cadena escrita en `appsettings`. El factory prioriza `ConnectionStrings__SaviaUp` sobre User Secrets y falla de forma explícita si ninguna fuente está configurada.

`appsettings.json` y `appsettings.Development.json` mantienen vacíos los campos sensibles. El `secrets.json` real reside fuera del repositorio bajo el almacén del perfil de usuario; no debe copiarse a esta solución.

Como alternativa para CI, producción o una sesión temporal, se admiten variables de entorno:

```text
ConnectionStrings__SaviaUp
Jwt__SigningKey
Jwt__Issuer
Jwt__Audience
Jwt__AccessTokenExpirationMinutes
Jwt__RefreshTokenExpirationDays
Email__Mode                 Development | Smtp
Email__Host / Port / Username / Password / FromAddress
Frontend__BaseUrl
Cors__AllowedOrigins__0
```

`appsettings.Development.json` usa correo de desarrollo: no envía mensajes y nunca registra el token ni el enlace completo.

## Base de datos y ejecución

```powershell
dotnet tool restore
dotnet restore
dotnet tool run dotnet-ef database update --project saviaup.backend.Infrastructure --startup-project saviaup.backend.Api
dotnet run --project saviaup.backend.Api
```

La API escucha en `http://localhost:5000`; Swagger queda en `/swagger` durante Development y el health check en `/health` valida aplicación y PostgreSQL.

La migración `InitialIdentityAndTenancy` crea:

```text
users, tenants, tenant_memberships, roles, modules, permissions,
role_permissions, refresh_tokens, password_reset_tokens, categories
```

Incluye índices únicos para email normalizado, membership `(UserId, TenantId)`, nombres normalizados de categoría por tenant, códigos de permisos, relación role/permission y hashes de tokens. Las relaciones sensibles usan eliminación `Restrict`. El seed contiene únicamente módulos y permisos globales. `AddCategoriesModule` agrega los permisos del módulo y `AddTenantCategories` crea el agregado funcional.

## Docker

```powershell
Copy-Item .env.example .env
# Reemplaza JWT_SIGNING_KEY en .env
docker compose up -d postgres
dotnet tool run dotnet-ef database update --project saviaup.backend.Infrastructure --startup-project saviaup.backend.Api
docker compose up --build api
```

Para aplicar la migración contra el contenedor desde el host, conserva el puerto 5432 y configura `ConnectionStrings__SaviaUp` como se indica arriba.

## Endpoints

```text
POST /api/auth/register
POST /api/auth/login
POST /api/auth/refresh
POST /api/auth/logout
POST /api/auth/forgot-password
POST /api/auth/reset-password

GET  /api/tenants
POST /api/tenants
POST /api/tenants/{tenantId}/select

GET  /api/users/me
GET  /api/users/me/info
GET  /api/modules/available

GET    /api/categories?includeInactive=false
POST   /api/categories
PUT    /api/categories/{categoryId}
PATCH  /api/categories/{categoryId}/status
DELETE /api/categories/{categoryId}

GET  /api/i18n/{language}
GET  /health
```

Los endpoints de tenants y `/users/me` requieren usuario y sesión válidos, pero funcionan sin tenant activo. `/api/users/me/info` y `/api/modules/available` requieren un token contextualizado con tenant y rol activos. Los futuros endpoints operativos pueden combinar `[RequireTenant]` y `[RequirePermission(PermissionCodes.X)]`.

### Contexto visible y navegación

`GET /api/users/me/info` entrega el contexto que se muestra después de ingresar:

```json
{
  "firstName": "Ana",
  "lastName": "Prueba",
  "organization": { "id": "...", "name": "Secret Garden" },
  "role": { "id": "...", "code": "TENANT_OWNER", "name": "Owner" }
}
```

`GET /api/modules/available` calcula la navegación desde los permisos actuales del rol. Un módulo activo aparece cuando el rol posee al menos uno de sus permisos, sin duplicarse aunque posea varios:

```json
{
  "sections": [
    {
      "code": "operation",
      "name": "Operación",
      "order": 2,
      "isGrouped": true,
      "modules": [
        { "id": "...", "code": "orders", "name": "Pedidos", "order": 1 },
        { "id": "...", "code": "reports", "name": "Reportes", "order": 2 },
        { "id": "...", "code": "billing", "name": "Facturación", "order": 3 }
      ],
      "options": []
    }
  ],
  "emptyStateMessage": null
}
```

Los nombres se localizan con `Accept-Language` (`es` o `en`, fallback español) y el frontend debe usar `code` como identificador estable. Secciones y módulos ya vienen ordenados. `isGrouped: false` indica que el único módulo debe mostrarse directamente, sin pestaña. Cuando el rol no tiene módulos u opciones, `sections` es `[]` y `emptyStateMessage` indica que debe contactar al administrador para gestionar los permisos.

Orden actual:

```text
1 Ventas: Mesas
2 Operación: Pedidos, Reportes, Facturación
3 Inventario: Productos, Categorías, Inventario, Cocina
4 Configuración: Configuración
```

La fuente de verdad es `Core/Navigation/NavigationCatalog.cs`. Cada módulo nuevo debe declarar sección/subcategoría y orden, además de seed, permisos, migración, copies y pruebas. El contrato incluye `options` para crecer con accesos administrativos respaldados por permisos `.manage`.

## Categorías

Las categorías se administran dentro del tenant activo y se mostrarán en frontend bajo `Inventario → Categorías`. El listado requiere `categories.read`; las mutaciones requieren `categories.manage`.

Creación y actualización usan:

```json
{
  "name": "Bebidas frías",
  "description": "Preparadas en barra",
  "imageUrl": "https://cdn.example.com/categories/drinks.webp",
  "isInventoryTracked": true
}
```

La respuesta agrega `id`, `isActive`, `createdAt` y `updatedAt`. `description` e `imageUrl` son opcionales; la imagen se representa como URL HTTP/HTTPS, no como archivo binario. Los nombres se comparan sin distinguir mayúsculas ni espacios sobrantes y no se repiten dentro de un tenant.

`GET /api/categories` devuelve solo activas para consumo de productos, inventarios y menús. La pantalla administrativa puede solicitar `includeInactive=true`. `PATCH /api/categories/{id}/status` recibe `{ "isActive": false }` y permite deshabilitar o reactivar. `DELETE` elimina físicamente la categoría mientras no existan relaciones restrictivas futuras.

## JWT, sesiones y multi-tenancy

El access token es corto y contiene `sub`, `jti`, `sid` y, cuando existe contexto, `tenant_id` y `role_id`. No contiene la lista de permisos.

Registro crea usuario y sesión sin tenant. Login reutiliza `LastTenantId` solo cuando todavía existe una membership activa; en otro caso devuelve `requiresTenantSelection: true`. Crear o seleccionar un tenant actualiza `LastTenantId`, conserva la sesión, revoca sus refresh tokens anteriores y entrega un par nuevo contextualizado.

Un usuario puede pertenecer a varios tenants mediante `TenantMembership`, con un rol diferente en cada uno. Al crear un tenant se generan atómicamente el tenant, rol de sistema `TENANT_OWNER`, membership del creador y todas las asignaciones de permisos disponibles.

## Refresh tokens y cierre de sesión

El refresh token es aleatorio, no es JWT y solo se devuelve una vez. PostgreSQL conserva SHA-256 del secreto, expiración, sesión y contexto. Cada refresh:

1. localiza el hash;
2. valida usuario y membership;
3. revoca el token anterior mediante actualización condicional dentro de una transacción;
4. crea un refresh nuevo y un access token;
5. enlaza `ReplacedByTokenId`.

La actualización condicional evita doble consumo concurrente. Logout revoca la sesión en backend; cambiar la contraseña revoca todas las sesiones del usuario.

## Autorización y errores

`PermissionAuthorizationMiddleware` valida sesión activa y metadata del endpoint. Responde:

- `401 AUTH_UNAUTHENTICATED`: JWT ausente/inválido o sesión revocada.
- `403 TENANT_REQUIRED`: endpoint tenant-aware sin contexto.
- `403 AUTH_FORBIDDEN`: usuario autenticado sin permiso.

Los permisos se resuelven en base de datos por `(TenantId, RoleId, PermissionCode)`. El formato uniforme es:

```json
{
  "success": false,
  "error": {
    "code": "AUTH_FORBIDDEN",
    "message": "No tienes permisos para realizar esta acción."
  }
}
```

El idioma se toma de `Accept-Language`, con fallback `es`. `/api/i18n/es` y `/api/i18n/en` son públicos. El middleware global agrega `X-Correlation-Id`, registra la excepción sin secretos y no expone stack traces.

## Recuperación de contraseña

Forgot-password siempre responde de forma neutral. El token es aleatorio, expira en una hora, es de un solo uso y solo se almacena hasheado. El enlace se construye desde `Frontend:BaseUrl`. Reset-password valida la política centralizada, marca el token mediante consumo condicional y revoca todas las sesiones.

## Pruebas y validación

```powershell
dotnet restore
dotnet build
dotnet test
```

`Core.Tests` cubre autenticación, tenants, permisos, navegación, información contextual y reglas CRUD de categorías. `IntegrationTests` arranca la API con EF InMemory y verifica el flujo completo, incluidos duplicados, edición, deshabilitación, filtros y eliminación de categorías, además del middleware 401/403 y membership activa.

El frontend conectado está en `../saviaup.frontend`: desarrollo usa `useMockApi: false` y `apiUrl: http://localhost:5000`.
