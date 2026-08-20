# AGENTS.md — Savia Up Backend

## Propósito

Este repositorio contiene el backend inicial de **Savia Up**, una plataforma SaaS multi-tenant para restaurantes y gastrobares. La solución cubre identidad, autenticación, sesiones, recuperación de contraseña, tenants, roles, permisos, internacionalización, persistencia PostgreSQL, documentación HTTP, health checks y pruebas.

Lee este archivo completo antes de modificar el repositorio. Las decisiones descritas aquí son invariantes del proyecto, no sugerencias opcionales.

## Stack obligatorio

- .NET 10 y C# con nullable reference types.
- ASP.NET Core con Controllers tradicionales.
- Entity Framework Core 10.
- PostgreSQL mediante Npgsql.
- JWT Bearer Authentication.
- Contenedor de dependencias integrado de Microsoft.
- xUnit para pruebas; Moq únicamente en proyectos de tests.
- Swagger/OpenAPI mediante Swashbuckle.

No introducir:

- AutoMapper.
- Autofac.
- MediatR.
- Minimal APIs para endpoints de negocio.
- ASP.NET Identity como framework completo.
- Generic Repository como abstracción central.
- Firebase, Supabase o Redis en esta fase.
- CQRS, Vertical Slice o una reorganización a Clean Architecture estándar.

## Solución y proyectos

La solución raíz es `saviaup.backend.sln` y contiene:

```text
saviaup.backend.Shared
saviaup.backend.Domain
saviaup.backend.Core
saviaup.backend.Infrastructure
saviaup.backend.Api
tests/saviaup.backend.Core.Tests
tests/saviaup.backend.IntegrationTests
```

Dirección de dependencias:

```text
Shared
  ↑
Domain
  ↑
Core

Infrastructure ──► Domain + Shared
Api ─────────────► Domain + Core + Infrastructure + Shared
Tests ───────────► proyectos bajo prueba
```

Reglas de dependencia:

- `Shared` es el nivel más bajo y solo contiene elementos realmente compartidos.
- `Domain` define entidades, DTO, resultados y puertos; no conoce EF, ASP.NET, SMTP ni Npgsql.
- `Core` implementa casos de uso y reglas; no conoce `HttpContext`, EF, Npgsql o Infrastructure.
- `Infrastructure` implementa puertos de salida: PostgreSQL, JWT, password hashing, tokens, email y tiempo.
- `Api` es el adapter de entrada HTTP, configura el host y traduce resultados a HTTP.
- No crear dependencias de Core hacia Infrastructure ni de Domain hacia Core.

Cada capa tiene su módulo de DI:

- `DomainModule.AddDomain()`.
- `CoreModule.AddCore()`.
- `InfrastructureModule.AddInfrastructure(configuration)`.
- `ApiModule.AddApi(configuration)` y `UseApi()`.

`Program.cs` debe mantenerse pequeño y dedicado a composición.

## Contenido por proyecto

### Shared

- `Constants/ClaimNames.cs`: `tenant_id`, `role_id`, `sid`.
- `Constants/ErrorCodes.cs`: códigos estables de negocio/HTTP.
- `Constants/HeaderNames.cs`: `X-Correlation-Id` y `X-Tenant-Id`.
- `Constants/PermissionCodes.cs`: códigos técnicos de permisos y catálogo actual.
- `Localization/LocalizationKeys.cs`: claves de copies.
- `Localization/TranslationCatalog.cs`: traducciones `es` y `en`, con fallback español.

Los códigos internos nunca se traducen. Solo se traduce el mensaje mostrado.

### Domain

Entidades actuales:

- `User`: email normalizado único, hash de password, idioma, `LastTenantId`, estado y timestamps.
- `Tenant`: organización activa/inactiva.
- `TenantMembership`: relación única `(UserId, TenantId)` y rol del usuario dentro del tenant.
- `Role`: siempre pertenece a un tenant y posee un `Code` estable.
- `Module`: módulo global funcional.
- `Permission`: permiso global identificado por `Code`.
- `RolePermission`: relación compuesta rol/permiso.
- `RefreshToken`: hash, sesión, contexto, expiración, revocación y reemplazo.
- `PasswordResetToken`: hash, expiración y consumo único.

Contratos HTTP/aplicación:

- Autenticación: login, registro, refresh, logout, forgot/reset password.
- Usuario: `UserDto`, tenant activo, rol y permisos efectivos para representación de UI.
- Tenants: listado, creación y respuesta de sesión contextualizada.
- Todos los contratos públicos son DTO; nunca se exponen entidades directamente.

Puertos:

- Repositorios específicos por agregado/uso: usuarios, tenants, roles, permisos y tokens.
- Servicios externos: `IPasswordHasher`, `IJwtTokenService`, `ITokenGenerator`, `IEmailSender`, `IDateTimeProvider`.
- Contexto: `ICurrentUserContext`.
- Casos de uso segregados por operación.
- `IUnitOfWork` para persistencia y transacciones.

El patrón `Result`/`Result<T>` representa errores esperados. No usar excepciones como flujo normal de negocio.

### Core

Casos de uso actuales:

- `LoginUseCase`.
- `RegisterUseCase`.
- `RefreshTokenUseCase`.
- `ForgotPasswordUseCase`.
- `ResetPasswordUseCase`.
- `LogoutUseCase`.
- `GetUserTenantsUseCase`.
- `CreateTenantUseCase`.
- `SelectTenantUseCase`.
- `GetCurrentUserUseCase`.
- `PermissionService`.

`SessionIssuer` centraliza la creación coordinada de access token, refresh token y DTO de sesión. `PasswordPolicy` centraliza todas las reglas de contraseña.

Reglas de Core:

- Todas las operaciones async reciben y propagan `CancellationToken`.
- No usar `.Result`, `.Wait()` o `.GetAwaiter().GetResult()`.
- Usar `IDateTimeProvider.UtcNow`; no dispersar `DateTime.UtcNow` en Core.
- Normalizar email con `Trim().ToUpperInvariant()`.
- Mapping explícito; no usar AutoMapper.
- Reglas de negocio en Core, validaciones HTTP simples cerca del contrato/API.

### Infrastructure

Persistencia:

- `SaviaUpDbContext` contiene los nueve `DbSet` actuales.
- Cada entidad tiene `IEntityTypeConfiguration<T>` independiente.
- Los repositorios usan consultas específicas, `AsNoTracking` para lectura y tracking solo cuando hay escritura.
- `UnitOfWork` abre transacciones para proveedores relacionales.
- `SaviaUpDbContextFactory` permite ejecutar `dotnet ef`.

La migración actual es `InitialIdentityAndTenancy` y crea:

```text
users
tenants
tenant_memberships
roles
modules
permissions
role_permissions
refresh_tokens
password_reset_tokens
```

Restricciones importantes:

- `users.normalized_email` único.
- `(tenant_memberships.user_id, tenant_id)` único.
- `(roles.tenant_id, code)` único.
- `modules.code` y `permissions.code` únicos.
- `(role_permissions.role_id, permission_id)` clave compuesta.
- hashes de refresh/reset tokens únicos.
- índices sobre usuarios, tenants, roles, sesiones y expiraciones.
- relaciones sensibles con `DeleteBehavior.Restrict`; preferir desactivación mediante `IsActive`.

`SeedData` contiene únicamente módulos y permisos globales. No agregar usuarios o tenants reales al seed.

Adapters de seguridad:

- `PasswordHasherAdapter` usa `PasswordHasher<User>` de Microsoft.
- `TokenGenerator` usa aleatoriedad criptográfica y SHA-256 para persistencia.
- `JwtTokenService` firma JWT HMAC SHA-256 y exige clave de al menos 32 bytes.
- `SystemDateTimeProvider` expone UTC.

Email:

- `DevelopmentEmailSender` no envía correo y nunca registra token o enlace completo.
- `EmailSender` usa SMTP configurado externamente.
- La selección se realiza con `Email:Mode = Development | Smtp`.

### Api

Controllers actuales:

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
GET  /api/i18n/{language}
GET  /health
```

`/api/i18n/{language}` es público. Los endpoints de auth sensibles usan rate limiting. Tenants y `/users/me` requieren autenticación y sesión activa, pero no exigen tenant seleccionado.

Componentes HTTP importantes:

- `CurrentUserContext`: encapsula lectura de `sub`, `sid`, `tenant_id` y `role_id`.
- `CorrelationIdMiddleware`: valida/genera `X-Correlation-Id`.
- `GlobalExceptionMiddleware`: registra errores inesperados y responde `INTERNAL_ERROR` sin stack trace.
- `PermissionAuthorizationMiddleware`: valida autenticación, sesión, tenant y permisos.
- `[RequireTenant]`: exige contexto tenant.
- `[RequirePermission(code)]`: exige permiso resuelto en backend.
- `ControllerResultExtensions`: convierte `Result` en status HTTP y respuesta localizada.
- `PostgresHealthCheck`: valida acceso a PostgreSQL.

Formato de error obligatorio:

```json
{
  "success": false,
  "error": {
    "code": "AUTH_FORBIDDEN",
    "message": "No tienes permisos para realizar esta acción.",
    "details": null
  }
}
```

Semántica:

- `401 AUTH_UNAUTHENTICATED`: JWT ausente/inválido/expirado o sesión revocada.
- `403 TENANT_REQUIRED`: autenticado sin tenant cuando el endpoint lo requiere.
- `403 AUTH_FORBIDDEN`: autenticado sin permiso.
- Nunca devolver 401 cuando corresponde 403.
- No devolver 200 para errores de negocio.

## Invariantes de autenticación y seguridad

### Contraseñas

- Nunca almacenar o registrar passwords en texto plano.
- Toda contraseña se procesa mediante `IPasswordHasher`.
- La política centralizada exige longitud, mayúscula, minúscula, número y carácter especial según configuración.
- Login no revela si falló email o password; ambos producen credenciales inválidas.

### JWT

El access token contiene como máximo el contexto de identidad/sesión:

```text
sub
jti
sid
tenant_id (opcional)
role_id (opcional)
email (informativo)
```

No incluir permisos en el JWT. La lista de permisos puede cambiar antes de que expire un access token y siempre debe comprobarse en backend.

### Refresh tokens

- No son JWT.
- Se generan con aleatoriedad criptográfica.
- Nunca se almacenan en texto plano; solo SHA-256.
- Tienen expiración, revocación y rotación.
- Están ligados a `UserId`, `SessionId` y contexto tenant/rol opcional.
- El consumo usa actualización condicional dentro de transacción para impedir doble refresh concurrente.
- `ReplacedByTokenId` conserva la cadena de rotación.
- Logout revoca la sesión en backend.
- Reset de contraseña revoca todas las sesiones del usuario.

No registrar access tokens, refresh tokens, reset tokens, claves JWT o credenciales SMTP.

## Invariantes multi-tenant

- Nunca asumir `1 User = 1 Tenant`.
- La pertenencia se resuelve exclusivamente mediante `TenantMembership`.
- El rol pertenece al tenant y puede variar para el mismo usuario entre tenants.
- Login usa `LastTenantId` solo si tenant, membership y rol siguen activos.
- Sin tenant válido se emite una sesión autenticada sin `tenant_id`/`role_id` y `requiresTenantSelection = true`.
- Crear tenant es atómico: tenant + `TENANT_OWNER` + membership + todos los permisos actuales + `LastTenantId` + tokens contextualizados.
- Seleccionar tenant valida membership, actualiza `LastTenantId`, revoca tokens previos de la sesión y entrega un par nuevo.
- El header `X-Tenant-Id`, si existe, debe coincidir con el claim; nunca sustituye al contexto firmado.
- La autorización depende de `(TenantId, RoleId, PermissionCode)`, nunca del nombre visible del rol.

## Contrato con el frontend

El frontend local está en `../saviaup.frontend` y consume `http://localhost:5000`.

- Login devuelve `AuthSessionDto` directamente.
- Registro devuelve `{ session, nextStep }`.
- Refresh devuelve `{ accessToken, refreshToken, expiresAt, accessTokenExpiresAt, refreshTokenExpiresAt }`.
- Crear/seleccionar tenant devuelve `{ tenant, tokens }`.
- El frontend debe persistir los nuevos tokens antes de navegar a `/app`.
- `expiresAt` representa la expiración del access token por compatibilidad con el frontend.
- Los nombres JSON usan camelCase.
- Si cambia un DTO o endpoint, actualizar pruebas de integración y el adaptador correspondiente del frontend en la misma tarea cuando esté en alcance.

## Configuración y secretos

Configuraciones principales:

```text
ConnectionStrings:SaviaUp
Jwt:Issuer
Jwt:Audience
Jwt:SigningKey
Jwt:AccessTokenExpirationMinutes
Jwt:RefreshTokenExpirationDays
PasswordPolicy:*
Email:*
Frontend:BaseUrl
Cors:AllowedOrigins
```

La firma JWT real debe provenir de variable de entorno, User Secrets o secret manager:

```powershell
$env:Jwt__SigningKey = "una-clave-aleatoria-de-al-menos-32-bytes"
```

No agregar secretos reales a `appsettings*.json`, `.env.example`, Dockerfile, Compose, pruebas o logs. `.env` está ignorado por Git.

CORS de producción debe enumerar orígenes explícitos; no usar `AllowAnyOrigin()`.

## Migraciones

Restaurar la herramienta local y aplicar migraciones:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update `
  --project saviaup.backend.Infrastructure `
  --startup-project saviaup.backend.Api
```

Crear una migración:

```powershell
dotnet tool run dotnet-ef migrations add NombreDescriptivo `
  --project saviaup.backend.Infrastructure `
  --startup-project saviaup.backend.Api `
  --output-dir Persistence/Migrations
```

Antes de crear una migración:

- Revisar el modelo y todas las configuraciones EF.
- Confirmar índices, unicidad, claves foráneas y comportamiento de eliminación.
- No editar una migración ya aplicada en ambientes compartidos; crear otra.
- Verificar el script idempotente cuando cambie el esquema.

## Ejecución local

```powershell
$env:Jwt__SigningKey = "una-clave-local-aleatoria-de-al-menos-32-bytes"
dotnet restore
dotnet tool restore
dotnet tool run dotnet-ef database update --project saviaup.backend.Infrastructure --startup-project saviaup.backend.Api
dotnet run --project saviaup.backend.Api
```

La API usa:

- HTTP: `http://localhost:5000`.
- HTTPS: `https://localhost:5001`.
- Swagger en Development: `/swagger`.
- Health check: `/health`.

Docker:

```powershell
Copy-Item .env.example .env
# Cambiar JWT_SIGNING_KEY por un secreto local seguro
docker compose up --build
```

`docker-compose.yml` levanta PostgreSQL y API. No agregar Redis en esta fase.

## Pruebas

Validación obligatoria antes de cerrar un cambio:

```powershell
dotnet restore saviaup.backend.sln
dotnet build saviaup.backend.sln --no-restore
dotnet test saviaup.backend.sln --no-build --no-restore
dotnet format saviaup.backend.sln --verify-no-changes --no-restore
```

Cobertura actual:

- Core: login correcto/inexistente/password erróneo/desactivado; registro correcto/duplicado; refresh correcto/expirado/revocado/reutilizado; reset correcto/inválido/expirado/usado; forgot neutral; selección de tenant y permisos.
- Integración: registro, login, refresh, listado/creación/selección de tenant, traducciones y middleware 401/403/request continúa.
- IntegrationTests reemplaza Npgsql por EF InMemory solo dentro del host de pruebas.

Al agregar o cambiar comportamiento:

- Añadir pruebas unitarias para reglas de Core.
- Añadir pruebas de integración para contratos, status codes, autenticación o middleware.
- Probar simultaneidad cuando cambie consumo de tokens.
- Mantener las pruebas deterministas; usar `IDateTimeProvider`.

## Estilo y mantenimiento

- Namespaces internos: `SaviaUp.Backend.*`.
- Preferir clases `sealed` cuando no existe un caso real de herencia.
- Mantener repositorios orientados a necesidades de dominio; no crear `IGenericRepository<T>`.
- Evitar servicios gigantes, Service Locator, dependencias estáticas y helpers genéricos sin cohesión.
- Usar UTC y `Guid` para identificadores externos.
- No realizar N+1 ni cargar grafos innecesarios.
- Mantener `Program.cs` y Controllers sin reglas de negocio.
- Mantener `Shared` pequeño; no usarlo como carpeta de utilidades generales.
- Conservar respuestas y códigos compatibles salvo que exista una migración explícita del contrato.
- No modificar archivos ajenos a la tarea ni descartar cambios del usuario.
- No crear commit o push salvo solicitud expresa del usuario.

## Checklist de cambio

Antes de entregar:

1. Confirmar que la dirección de dependencias sigue siendo válida.
2. Confirmar que no se exponen entidades, hashes o secretos.
3. Revisar multi-tenancy y pertenencia del rol al tenant.
4. Revisar 401 vs 403 y códigos estables.
5. Revisar transacción y concurrencia si hay múltiples escrituras.
6. Revisar índices/migración si cambia persistencia.
7. Actualizar DTO, OpenAPI, frontend y documentación si cambia el contrato.
8. Ejecutar restore, build, tests y formato.
9. Verificar `git diff` y buscar secretos antes de commit.

## Documentación adicional

`README.md` contiene la guía de uso para desarrolladores y operadores. Este `AGENTS.md` contiene las reglas de implementación para agentes y colaboradores. Si ambos divergen, corregirlos dentro del mismo cambio.
