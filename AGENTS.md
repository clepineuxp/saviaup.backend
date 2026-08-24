# AGENTS.md — Savia Up Backend

## Propósito

Este repositorio contiene el backend inicial de **Savia Up**, una plataforma SaaS multi-tenant para restaurantes y gastrobares. La solución cubre identidad, autenticación, sesiones, recuperación de contraseña, organizaciones, roles, permisos, navegación, configuración organizacional y de negocio, medios de pago, invitaciones, categorías, productos, inventario, ingredientes, movimientos, complementos, salas, mesas, operación en tiempo real, internacionalización, persistencia PostgreSQL, documentación HTTP, health checks y pruebas.

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
Todo módulo nuevo del seed debe agregar sus copies `modules.{code}` en español e inglés, su sección/orden en `NavigationCatalog` y pruebas de localización/orden.

### Domain

Entidades actuales:

- `User`: email normalizado único, hash de password, idioma, `LastTenantId`, estado y timestamps.
- `Tenant`: organización activa/inactiva.
- `TenantMembership`: relación única `(UserId, TenantId)` y rol del usuario dentro del tenant.
- `Role`: siempre pertenece a un tenant y posee un `Code` estable.
- `Module`: módulo global funcional.
- `Permission`: permiso global identificado por `Code`.
- `RolePermission`: relación compuesta rol/permiso.
- `Category`: categoría funcional perteneciente a un tenant, con nombre normalizado, descripción/imagen opcionales, clasificación inventariable, estado y timestamps.
- `Product`: producto tenant-aware de tipo `NORMAL` o `COMBO`, con categoría obligatoria, precio de venta, descripción/imagen/tiempo de preparación opcionales, clasificación inventariable, estado y timestamps.
- `DiningArea`: sala tenant-aware con nombre normalizado único, orden único y estado activo.
- `RestaurantTable`: mesa tenant-aware asignada a una sala, con capacidad, coordenadas 2D, forma, flags de domicilio/caja, estado y referencia opcional a la orden activa.
- `CashRegisterShift`: hook persistente mínimo para conocer si existe un turno de caja abierto cuando el tenant lo exige.
- `OrganizationParameter`: parámetro extensible y tipado por organización, identificado por una clave estable.
- `PaymentMethod`: medio de pago tenant-aware con nombre único, estado y participación en apertura de caja.
- `TenantPermission`: catálogo de permisos globales habilitados explícitamente para una organización.
- `TenantInvitation`: invitación por correo a una organización y rol, pendiente hasta que se registre la cuenta.
- `MeasurementUnit`: unidad perteneciente a un tenant, con código/nombre normalizados, estado y timestamps.
- `Ingredient`: ingrediente tenant-aware con categoría y unidad obligatorias, stock mínimo/actual, estado y timestamps.
- `InventoryMovement`: registro inmutable de entrada/salida, motivo, cantidad, stock anterior/posterior, usuario creador y fecha.
- `RefreshToken`: hash, sesión, contexto, expiración, revocación y reemplazo.
- `PasswordResetToken`: hash, expiración y consumo único.

Contratos HTTP/aplicación:

- Autenticación: login, registro, refresh, logout, forgot/reset password.
- Usuario: `UserDto` para sesión y `UserInfoDto` para nombre, apellido, organización y rol actuales.
- Navegación: `AvailableModulesResponse`, secciones, módulos/opciones ordenados y copy de estado vacío.
- Categorías: listado y contratos de creación, actualización y cambio de estado.
- Productos: listado paginado y contratos de creación, actualización y cambio de estado.
- Inventario: listados paginados de existencias, ingredientes, movimientos y unidades; contratos de creación/edición/estado para ingredientes y unidades, y creación de movimientos.
- Mesas: CRUD de salas/mesas, reordenamiento, snapshot operativo, apertura/liberación, actualización de total y eventos SignalR.
- Tenants: listado, creación y respuesta de sesión contextualizada.
- Todos los contratos públicos son DTO; nunca se exponen entidades directamente.

Puertos:

- Repositorios específicos por agregado/uso: usuarios, tenants, roles, permisos, módulos, categorías, ingredientes, movimientos, unidades y tokens.
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
- `GetUserInfoUseCase`.
- `GetAvailableModulesUseCase`.
- `ListCategoriesUseCase`.
- `CreateCategoryUseCase`.
- `UpdateCategoryUseCase`.
- `SetCategoryStatusUseCase`.
- `DeleteCategoryUseCase`.
- `ListProductsUseCase`, `CreateProductUseCase`, `UpdateProductUseCase`, `SetProductStatusUseCase`, `DeleteProductUseCase`.
- casos de uso de salas, mesas y operación agrupados bajo `Core/Tables`, incluido el bloqueo por turno de caja.
- `ListInventoryUseCase`.
- `ListIngredientsUseCase`, `CreateIngredientUseCase`, `UpdateIngredientUseCase`, `SetIngredientStatusUseCase`, `DeleteIngredientUseCase`.
- `ListInventoryMovementsUseCase`, `CreateInventoryMovementUseCase`.
- `ListMeasurementUnitsUseCase`, `CreateMeasurementUnitUseCase`, `UpdateMeasurementUnitUseCase`, `SetMeasurementUnitStatusUseCase`, `DeleteMeasurementUnitUseCase`.
- `PermissionService`.
- `OrganizationSettingsUseCase`, `BusinessSettingsUseCase`, `PaymentMethodsSettingsUseCase` y `AccessSettingsUseCase`.

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

- `SaviaUpDbContext` contiene los veintiún `DbSet` actuales.
- Cada entidad tiene `IEntityTypeConfiguration<T>` independiente.
- Los repositorios usan consultas específicas, `AsNoTracking` para lectura y tracking solo cuando hay escritura.
- `ModuleRepository` devuelve un módulo activo cuando el rol activo posee al menos un permiso de ese módulo dentro del tenant solicitado.
- `NavigationCatalog` en Core es la fuente de verdad de presentación: subcategoría/sección y orden de cada módulo, además de opciones administrativas extensibles.
- `UnitOfWork` abre transacciones para proveedores relacionales.
- `SaviaUpDbContextFactory` permite ejecutar `dotnet ef`.

`InitialIdentityAndTenancy` crea:

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
`AddCategoriesModule` agrega el módulo `categories`, los permisos `categories.read`/`categories.manage` y los asigna a los roles `TENANT_OWNER` existentes.
`AddTenantCategories` crea la tabla tenant-aware `categories`, su FK restrictiva e índices de unicidad/listado.
`AddInventoryManagement` crea `measurement_units`, `ingredients` e `inventory_movements`, agrega los siete permisos granulares de inventario, los asigna a `TENANT_OWNER` existentes y crea `gr`, `kg` y `und` para tenants existentes.
`AddTenantProducts` crea `products`, sus FKs restrictivas hacia tenant/categoría, restricciones de tipo/precio/tiempo e índices tenant-aware para los filtros del listado.
`AddProductTypeDefault` fija `NORMAL` como valor por defecto de `products.Type` también a nivel PostgreSQL.
`AddTableManagement` crea salas, mesas y turnos de caja, agrega `tables.operate`, lo asigna a propietarios existentes y añade `RequiresOpenCashRegister` al tenant.
`AddOrganizationSettings` amplía la organización, crea parámetros, medios de pago, invitaciones y permisos habilitados por organización; hace backfill de parámetros/permisos, conserva los administradores con `settings.manage` y no crea medios de pago en organizaciones existentes.

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
GET  /api/users/me/info
GET  /api/modules/available

GET    /api/categories?includeInactive=false
POST   /api/categories
PUT    /api/categories/{categoryId}
PATCH  /api/categories/{categoryId}/status
DELETE /api/categories/{categoryId}

GET    /api/products?page=1&pageSize=20&search=&categoryId=&type=&includeInactive=false
POST   /api/products
PUT    /api/products/{productId}
PATCH  /api/products/{productId}/status
DELETE /api/products/{productId}

GET/POST   /api/table-areas
PUT/DELETE /api/table-areas/{areaId}
PUT        /api/table-areas/reorder
GET/POST   /api/tables
PUT/DELETE /api/tables/{tableId}
GET        /api/tables/operation
PATCH      /api/tables/{tableId}/operation
PATCH      /api/tables/{tableId}/order
SIGNALR    /hubs/tables

GET    /api/inventory?page=1&pageSize=20&search=&belowMinimum=
GET    /api/inventory/ingredients?page=1&pageSize=20&search=&categoryId=&includeInactive=false
POST   /api/inventory/ingredients
PUT    /api/inventory/ingredients/{ingredientId}
PATCH  /api/inventory/ingredients/{ingredientId}/status
DELETE /api/inventory/ingredients/{ingredientId}

GET  /api/inventory/movements?page=1&pageSize=20&ingredientId=&direction=
POST /api/inventory/movements

GET    /api/inventory/complements/units?page=1&pageSize=20&search=&includeInactive=false
POST   /api/inventory/complements/units
PUT    /api/inventory/complements/units/{unitId}
PATCH  /api/inventory/complements/units/{unitId}/status
DELETE /api/inventory/complements/units/{unitId}

GET  /api/i18n/{language}
GET  /health
GET  /healthz
GET  /api/health

GET  /api/orders?page=1&pageSize=25&search=&statuses=&fromDate=&toDate=&tableId=
GET  /api/orders/items?page=1&pageSize=25&search=&statuses=&fromDate=&toDate=&tableId=
GET  /api/orders/table/{tableId}/active
POST /api/orders/table/{tableId}/items
POST /api/orders/table/{tableId}/move
POST /api/orders/items/{itemId}/cancel
POST /api/orders/table/{tableId}/checkout
POST /api/orders/{orderId}/receipts/summary
GET  /api/orders/{orderId}/receipts

GET/PUT/POST/DELETE /api/settings/organization[/logo]
GET/PUT             /api/settings/business
GET/POST            /api/settings/payment-methods
PUT/PATCH/DELETE    /api/settings/payment-methods/{paymentMethodId}[/status]
GET                 /api/settings/access/permissions
GET/POST            /api/settings/access/roles
PUT/PATCH/DELETE    /api/settings/access/roles/{roleId}[/status]
GET/POST            /api/settings/access/users
PATCH/DELETE        /api/settings/access/users/{entryId}
```

`/api/i18n/{language}` es público. Los endpoints de auth sensibles usan rate limiting. Tenants y `/users/me` requieren autenticación y sesión activa, pero no exigen tenant seleccionado. `/users/me/info` y `/modules/available` exigen tenant y rol activos mediante `[RequireTenant]`.

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
- Crear tenant es atómico: tenant + `TENANT_OWNER` + membership + todos los permisos actuales + unidades `gr`/`kg`/`und` + `LastTenantId` + tokens contextualizados.
- Seleccionar tenant valida membership, actualiza `LastTenantId`, revoca tokens previos de la sesión y entrega un par nuevo.
- El header `X-Tenant-Id`, si existe, debe coincidir con el claim; nunca sustituye al contexto firmado.
- La autorización depende de `(TenantId, RoleId, PermissionCode)`, nunca del nombre visible del rol.
- Todo endpoint con `[RequireTenant]` vuelve a comprobar membership, tenant y rol activos antes de continuar; un claim firmado no sustituye esa validación actual.
- La navegación se resuelve para el rol del token contextual; nunca acepta `tenantId` o `roleId` enviados por el cliente.
- Antes de resolver módulos se vuelve a validar que membership, tenant y rol continúen activos y que el rol coincida con el claim.
- Un módulo se habilita si el rol tiene al menos un permiso perteneciente a ese módulo. No se requieren todos sus permisos.
- Solo se devuelven módulos y roles activos. La respuesta no duplica módulos aunque existan múltiples permisos asignados.
- `NavigationCatalog` ordena secciones y módulos con enteros positivos independientes. El orden de la base de datos no controla la UI.
- Una sección se omite si el rol no tiene elementos disponibles en ella.
- `isGrouped` es `true` solo cuando la sección visible contiene más de un módulo/opción; con un único módulo el frontend debe renderizar ese acceso directamente, sin pestaña contenedora.
- Las opciones futuras se declaran en la sección con código, módulo propietario, orden y permiso requerido terminado en `.manage`; el backend solo devuelve las autorizadas.
- Si no hay módulos ni opciones disponibles, `sections` queda vacío y `emptyStateMessage` contiene el copy localizado para contactar al administrador.

Configuración actual de navegación:

```text
1 sales / Ventas
  1 tables
2 operation / Operación
  1 orders
  2 reports
  3 billing
3 inventory / Inventario
  1 products
  2 categories
  3 inventory
  4 kitchen
4 configuration / Configuración
  1 settings
  2 tables.manage (opción administrativa)
```

Al crear un módulo nuevo es obligatorio, dentro del mismo cambio:

1. Declarar módulo y permisos en `SeedData`/`PermissionCodes` y crear una migración.
2. Declarar exactamente una sección/subcategoría y un orden de módulo único en `NavigationCatalog`.
3. Declarar la sección y su orden si es nueva.
4. Agregar copies `es`/`en` para módulo, sección y cualquier opción.
5. Actualizar pruebas unitarias, integradas y documentación.

El catálogo falla explícitamente si la base de datos devuelve un módulo sin configuración de navegación. Los órdenes duplicados dentro de una sección tampoco son válidos.

## Invariantes de categorías

- Toda categoría pertenece exactamente a un tenant y todas las consultas reciben `TenantId` desde el claim, nunca desde body/query.
- `categories.read` autoriza el listado; `categories.manage` autoriza creación, actualización, cambio de estado y eliminación.
- El nombre visible se recorta y colapsa espacios internos. `NormalizedName` usa mayúsculas y soporta unicidad case-insensitive dentro del tenant.
- El índice único `(TenantId, NormalizedName)` permite repetir un nombre en tenants diferentes, pero no dentro de la misma organización, incluso si cambia mayúsculas o espacios.
- `Description` es opcional y tiene máximo 1000 caracteres.
- `ImageUrl` es opcional, admite únicamente URL absoluta HTTP/HTTPS y máximo 2048 caracteres. La API no recibe ni almacena binarios en esta fase.
- `IsInventoryTracked` indica si los productos de esa categoría participan en inventario.
- `IsActive` es reversible mediante `PATCH /status`. El listado normal omite categorías inactivas; administración usa `includeInactive=true`.
- `DELETE` realiza eliminación física solo cuando no hay ingredientes ni productos asociados; en caso contrario devuelve `CATEGORY_IN_USE`. Las FK usan `Restrict` y debe preferirse desactivar.
- Crear/actualizar valida duplicados en Core y la base de datos conserva la restricción como última barrera.

## Invariantes de productos

- Todo producto pertenece exactamente a un tenant y a una categoría de ese mismo tenant. `TenantId` se obtiene siempre del claim autenticado y nunca del body/query.
- `products.read` autoriza el listado; `products.manage` autoriza creación, actualización, cambio de estado y eliminación.
- `Type` solo admite `NORMAL` o `COMBO`, sin distinguir mayúsculas/minúsculas en la entrada. Si se omite al crear o actualizar, el valor efectivo es `NORMAL`.
- El nombre es obligatorio, se recorta y colapsa espacios internos, y tiene máximo 120 caracteres. No existe una restricción de unicidad de nombre para productos.
- `SalePrice` es obligatorio, positivo y usa precisión `numeric(18,2)`. `PreparationTimeMinutes` es opcional y no negativo.
- `Description` es opcional y tiene máximo 1000 caracteres. `ImageUrl` es opcional, admite solo URL absoluta HTTP/HTTPS y tiene máximo 2048 caracteres; no se reciben binarios ni multipart en esta fase.
- La categoría debe estar activa. `IsInventoryTracked` solo puede ser `true` cuando la categoría elegida es inventariable; Core lo fuerza a `false` si la categoría no lo permite, tanto al crear como al actualizar. Si una categoría deja de ser inventariable, todos sus productos se actualizan a `false` en la misma persistencia.
- Todos los listados son paginados y aceptan búsqueda por nombre, filtro por categoría, filtro por tipo e `includeInactive`. Toda consulta filtra explícitamente por tenant.
- `IsActive` cambia de forma reversible mediante `PATCH /status`. El listado normal omite productos inactivos.
- `DELETE` elimina físicamente el producto actual mientras no existan restricciones de uso futuras. Las relaciones con tenant y categoría usan `Restrict`.

## Invariantes de inventario

- Todos los endpoints toman `TenantId` del contexto autenticado. Ningún body/query puede elegir tenant y todo acceso a ingrediente, categoría, unidad o movimiento filtra explícitamente por tenant.
- Los cuatro apartados son capacidades dentro del módulo global `inventory`; no agregan módulos de navegación independientes.
- Permisos: `inventory.stock.read`; `inventory.ingredients.read/manage`; `inventory.movements.read/manage`; `inventory.complements.read/manage`. Lectura y mutación nunca comparten implícitamente permiso.
- Todos los listados usan `{ items, page, pageSize, totalCount, totalPages }`; `page >= 1` y `pageSize` está entre 1 y 100.
- Un ingrediente exige categoría activa del mismo tenant, unidad activa del mismo tenant y nombre. Descripción es opcional; `MinimumStock` es no negativo y por defecto `0`.
- `InitialStock` solo existe al crear el ingrediente. Si es mayor que cero, se genera atómicamente un movimiento interno `increase/initial`; las ediciones posteriores nunca modifican stock directamente.
- `CurrentStock` solo cambia mediante `CreateInventoryMovementUseCase`. Los movimientos son inmutables: no existen endpoints de edición o eliminación.
- Entradas públicas válidas: `purchase`, `production`, `acquisition`. Salidas válidas: `expiration`, `loss`, `waste`. Cada motivo solo acepta su dirección correspondiente.
- Toda cantidad es positiva y usa precisión `numeric(18,3)`. Una salida que supera el stock actual devuelve `INVENTORY_INSUFFICIENT_STOCK` con HTTP 422 y nunca deja stock negativo.
- La actualización de stock se ejecuta en transacción y bloquea la fila en proveedores relacionales antes de calcular `StockBefore`/`StockAfter`.
- El inventario actual lista únicamente ingredientes activos cuya categoría activa tiene `IsInventoryTracked = true`. `IsBelowMinimum` significa estrictamente `CurrentStock < MinimumStock`.
- Las unidades son el primer tipo de complemento; las rutas se anidan bajo `/api/inventory/complements/units` para poder añadir otros complementos sin romper el contrato.
- Cada tenant nuevo recibe `gr`/`gramos`, `kg`/`kilogramos` y `und`/`unidades`; la migración hace backfill a tenants existentes. Código y nombre son únicos, normalizados y case-insensitive dentro de cada tenant.
- No se elimina físicamente un ingrediente con movimientos, una unidad usada o una categoría usada. Se devuelve conflicto estable y se ofrece desactivación reversible.

## Invariantes de configuración

- La interfaz y los copies públicos usan siempre **organización**; `Tenant` permanece únicamente como nombre técnico interno.
- La información, parámetros, medios de pago, roles, membresías e invitaciones se filtran por el `TenantId` firmado. Ningún request puede escoger otra organización.
- Permisos granulares: `settings.organization.read/manage`, `settings.business.read/manage`, `settings.payment-methods.read/manage`, `settings.users.read/manage` y `settings.roles.read/manage`. `settings.manage` se conserva solo por compatibilidad y no se ofrece al crear roles nuevos.
- `tenant_permissions` define el límite máximo de capacidades de una organización. La autorización, navegación y asignación de roles exigen que el permiso esté habilitado allí.
- Solo `TENANT_OWNER` puede modificar el documento de la organización. El rol propietario del sistema no se edita, desactiva ni elimina y siempre debe existir al menos una membresía propietaria activa.
- El logo se persiste como `bytea`, admite únicamente PNG/JPEG/WebP, máximo 2 MB, y se entrega por un endpoint autenticado. No registrar contenido ni nombres sensibles.
- Los parámetros usan claves estables y valor tipado para crecer sin agregar columnas. Las seis claves iniciales viven en `SettingsDefaults`; `RequiresOpenCashRegister` se sincroniza también con `Tenant` por compatibilidad operativa.
- Cada organización nueva recibe `Efectivo`, `Tarjeta/Datafono` y `Transferencia`; la migración no hace backfill de medios de pago en organizaciones existentes.
- Las invitaciones se normalizan por correo y son únicas por organización. Si la cuenta ya existe se crea la membresía inmediatamente; si no existe queda `PENDING` y el registro posterior crea la membresía y marca la invitación aceptada. `IEmailSender` notifica un enlace de login/registro sin registrar la URL completa en modo Development.
- Una membresía puede deshabilitarse indefinidamente (`IsActive=false`, `DisabledUntil=null`) o hasta una fecha. Al vencer la fecha recupera acceso sin intervención y todos los flujos de autenticación/autorización aplican la misma regla.
- Los roles personalizados solo reciben permisos habilitados para la organización. Un rol asignado no puede eliminarse; debe desactivarse o reasignar primero a sus usuarios.

## Invariantes de salas y mesas

- Toda sala, mesa, turno de caja y consulta operativa se filtra por el `TenantId` firmado; ningún body puede escoger tenant.
- `tables.read` permite consultar el snapshot y conectarse al hub; `tables.operate` permite abrir, actualizar y liberar mesas; `tables.manage` autoriza el CRUD de configuración.
- Nombre de sala es único por tenant; nombre de mesa es único dentro de la sala. Ambos se normalizan en Core y tienen índices únicos.
- Las coordenadas admiten `-100000..100000`, la capacidad `1..100` y los totales nunca son negativos.
- `Shape` solo admite `SQUARE`, `ROUND`, `RECTANGLE_HORIZONTAL` o `RECTANGLE_VERTICAL`; las mesas existentes usan `SQUARE` por defecto y el DTO conserva esos valores estables.
- Una mesa `DISABLED` no se opera. Una mesa ocupada no se elimina. El estado `AVAILABLE` siempre limpia orden, total y tiempo de ocupación.
- Una mesa `IsCashRegister` procesa pedidos inmediatos sin conservar ocupación persistente.
- Si `Tenant.RequiresOpenCashRegister` es verdadero, toda mutación operativa exige un `CashRegisterShift` sin `ClosedAt`.
- `TablesHub` publica `OnTableStatusChanged` y `OnTableOrderUpdated` únicamente al grupo derivado del tenant autenticado.

## Contrato con el frontend

El frontend local está en `../saviaup.frontend` y consume `http://localhost:5000`.

- Login devuelve `AuthSessionDto` directamente.
- Registro devuelve `{ session, nextStep }`.
- Refresh devuelve `{ accessToken, refreshToken, expiresAt, accessTokenExpiresAt, refreshTokenExpiresAt }`.
- Crear/seleccionar tenant devuelve `{ tenant, tokens }`.
- `GET /api/users/me/info` devuelve `{ firstName, lastName, organization, role }` para el contexto activo.
- `GET /api/modules/available` devuelve `{ sections, emptyStateMessage }`.
- Cada sección contiene `{ code, name, order, isGrouped, modules, options }`; cada módulo contiene `{ id, code, name, order }`.
- Las opciones usan `{ code, moduleCode, name, order }`; actualmente el catálogo no publica opciones, pero el contrato queda preparado para opciones `.manage` futuras.
- `name` y `emptyStateMessage` se localizan con `Accept-Language` (`es`/`en`, fallback `es`). Los valores `code` son estables y nunca se traducen.
- Con al menos una sección, `emptyStateMessage` es `null`; sin elementos disponibles, el frontend debe mostrar ese copy en lugar de una navegación vacía.
- El frontend debe respetar los órdenes recibidos y no recrear agrupaciones ni ordenarlas alfabéticamente.
- El frontend debe persistir los nuevos tokens antes de navegar a `/app`.
- `expiresAt` representa la expiración del access token por compatibilidad con el frontend.
- Los nombres JSON usan camelCase.
- Las cuatro vistas de inventario consumen exclusivamente los endpoints paginados y habilitan acciones según los permisos granulares, no según el nombre del rol.
- La vista `/app/products` consume exclusivamente el listado paginado, carga categorías activas desde `/api/categories` y habilita mutaciones con `products.manage`; el selector requiere además `categories.read`.
- El formulario de producto inicia `type` en `NORMAL` y deshabilita/fuerza `isInventoryTracked=false` cuando la categoría elegida no es inventariable. El backend repite siempre esta validación.
- El prompt de adaptación del frontend se entrega únicamente en conversación; no se versiona un archivo `.md` de prompt.
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

Para desarrollo local, API e Infrastructure comparten el mismo `UserSecretsId`. Esto permite que el host cargue los secretos al ejecutar la API y que `SaviaUpDbContextFactory` lea la misma cadena al crear o aplicar migraciones:

```powershell
dotnet user-secrets set "ConnectionStrings:SaviaUp" "Host=localhost;Port=5432;Database=saviaup;Username=usuario;Password=clave" --project saviaup.backend.Api
dotnet user-secrets set "Jwt:SigningKey" "una-clave-aleatoria-de-al-menos-32-bytes" --project saviaup.backend.Api
dotnet user-secrets list --project saviaup.backend.Api
```

El archivo físico de User Secrets vive fuera del repositorio. No crear ni versionar un `secrets.json` dentro de la solución. `SaviaUpDbContextFactory` usa primero `ConnectionStrings__SaviaUp` si existe y, en caso contrario, `ConnectionStrings:SaviaUp` desde User Secrets; no posee cadena PostgreSQL de fallback.

`appsettings*.json` debe conservar vacíos `ConnectionStrings:SaviaUp`, `Jwt:SigningKey` y cualquier credencial. En producción usar variables de entorno o un secret manager. No agregar secretos reales a `appsettings*.json`, `.env.example`, Dockerfile, Compose, pruebas o logs. `.env` está ignorado por Git.

Los proyectos `saviaup.backend.Api` y `saviaup.backend.Infrastructure` deben conservar el mismo `UserSecretsId`; si cambia, actualizar ambos dentro del mismo commit.

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
dotnet user-secrets set "ConnectionStrings:SaviaUp" "Host=localhost;Port=5432;Database=saviaup;Username=usuario;Password=clave" --project saviaup.backend.Api
dotnet user-secrets set "Jwt:SigningKey" "una-clave-local-aleatoria-de-al-menos-32-bytes" --project saviaup.backend.Api
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

- Core: login correcto/inexistente/password erróneo/desactivado; registro correcto/duplicado; refresh correcto/expirado/revocado/reutilizado; reset correcto/inválido/expirado/usado; forgot neutral; selección de tenant; permisos; navegación; información de usuario; CRUD, normalización, duplicados, validación y estado de categorías; reglas, filtros, estado y CRUD de productos.
- Integración: registro, login, refresh, tenants, navegación, información de usuario, flujos completos de categorías y productos, traducciones y middleware 401/403/membership revocada/request continúa.
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
- Los prompts solicitados para otros agentes se entregan en la conversación y no se guardan en el repositorio salvo petición explícita de crear un archivo.

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
