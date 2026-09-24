# Savia Up Backend

Backend inicial de Savia Up, una plataforma SaaS multi-tenant para restaurantes y gastrobares. Está implementado en .NET 10, ASP.NET Core Controllers, Entity Framework Core 10 y PostgreSQL, con arquitectura hexagonal (Ports & Adapters).

> Control de versiones: no se crean commits ni se hace push salvo solicitud explícita del usuario en el mensaje actual.

## Fechas y zonas horarias

Los instantes se almacenan en UTC (`DateTimeOffset`/`timestamptz`) y cada organización usa su `TimeZoneId` IANA. Los filtros por día reciben `yyyy-MM-dd` y se resuelven en el backend como rangos UTC de la organización. La auditoría, riesgos para históricos y scripts idempotentes están en [docs/time-zones-audit.md](docs/time-zones-audit.md).

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
dotnet user-secrets set "ConnectionStrings:PlatformDatabase" "Host=localhost;Port=5432;Database=saviaup_platform;Username=usuario;Password=clave" --project saviaup.backend.Api
dotnet user-secrets set "ConnectionStrings:ApplicationDatabase" "Host=localhost;Port=5432;Database=saviaup_application;Username=usuario;Password=clave" --project saviaup.backend.Api
dotnet user-secrets set "Jwt:SigningKey" "una-clave-local-aleatoria-de-al-menos-32-bytes" --project saviaup.backend.Api
dotnet user-secrets list --project saviaup.backend.Api
```

API e Infrastructure comparten el mismo `UserSecretsId`. El host usa ese almacén al ejecutar en Development y los factories lo usan directamente durante `dotnet ef`, de modo que las migraciones no dependen de cadenas escritas en `appsettings`. Los factories priorizan `ConnectionStrings__PlatformDatabase` y `ConnectionStrings__ApplicationDatabase` sobre User Secrets y fallan de forma explícita si falta su conexión.

`appsettings.json` y `appsettings.Development.json` mantienen vacíos los campos sensibles. El `secrets.json` real reside fuera del repositorio bajo el almacén del perfil de usuario; no debe copiarse a esta solución.

Como alternativa para CI, producción o una sesión temporal, se admiten variables de entorno:

```text
ConnectionStrings__PlatformDatabase
ConnectionStrings__ApplicationDatabase
Jwt__SigningKey
Jwt__Issuer
Jwt__Audience
Jwt__AccessTokenExpirationMinutes
Jwt__RefreshTokenExpirationDays
Email__Mode                 Development | Smtp
Email__Host / Port / Username / Password / FromAddress
Frontend__BaseUrl
Cors__AllowedOrigins__0
Printing__AgentDownloadUrl
Printing__PairingCodeExpirationMinutes
Printing__DeviceTokenExpirationDays
Printing__HeartbeatIntervalSeconds
Printing__OfflineTimeoutSeconds
Printing__HeartbeatPersistenceSeconds
```

`appsettings.Development.json` usa correo de desarrollo: no envía mensajes y nunca registra el token ni el enlace completo.

## Base de datos y ejecución

```powershell
dotnet tool restore
dotnet restore
dotnet tool run dotnet-ef database update --context PlatformDbContext --project saviaup.backend.Infrastructure --startup-project saviaup.backend.Api
dotnet tool run dotnet-ef database update --context ApplicationDbContext --project saviaup.backend.Infrastructure --startup-project saviaup.backend.Api
dotnet run --project saviaup.backend.Api
```

La API escucha en `http://localhost:5000`; Swagger queda en `/swagger` durante Development y el health check en `/health` valida aplicación y PostgreSQL.

El historial de migraciones crea:

```text
users, tenants, tenant_memberships, roles, modules, permissions,
role_permissions, refresh_tokens, password_reset_tokens, categories,
measurement_units, ingredients, inventory_movements, products,
product_recipe_items, dining_areas, restaurant_tables, orders,
order_items, order_receipts, cash_registers, cash_register_shifts,
cash_register_shift_movements, expenses, suppliers, stored_images,
organization_parameters, payment_methods, tenant_permissions, tenant_invitations
locations, print_agents, print_agent_credentials, print_agent_pairing_codes,
print_agent_discovered_printers, printers, printing_zones, printing_zone_printers, category_printing_routes,
product_printing_routes, print_jobs
```

Incluye índices únicos para email normalizado, membership `(UserId, TenantId)`, nombres normalizados de categoría/unidad por tenant, códigos de permisos, relación role/permission y hashes de tokens. Las relaciones sensibles usan eliminación `Restrict`. El seed contiene únicamente módulos y permisos globales. Las migraciones crean y actualizan la infraestructura multi-tenant completa, turnos de caja, salas y mesas, comandas, recetas de productos, facturación, estadísticas, gastos y proveedores.

## Docker

```powershell
Copy-Item .env.example .env
# Reemplaza JWT_SIGNING_KEY en .env
docker compose up -d postgres
dotnet tool run dotnet-ef database update --context PlatformDbContext --project saviaup.backend.Infrastructure --startup-project saviaup.backend.Api
dotnet tool run dotnet-ef database update --context ApplicationDbContext --project saviaup.backend.Infrastructure --startup-project saviaup.backend.Api
docker compose up --build api
```

Para aplicar las migraciones contra el contenedor desde el host, conserva el puerto 5432 y configura ambas cadenas como se indica arriba.

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

GET    /api/products?page=1&pageSize=20&search=&categoryId=&type=&includeInactive=false
POST   /api/products
PUT    /api/products/{productId}
PATCH  /api/products/{productId}/status
DELETE /api/products/{productId}

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

GET    /api/dining-areas
POST   /api/dining-areas
PUT    /api/dining-areas/{id}
DELETE /api/dining-areas/{id}

GET    /api/tables
POST   /api/tables
PUT    /api/tables/{id}
PATCH  /api/tables/{id}/status
DELETE /api/tables/{id}
GET    /api/tables/operation
GET    /api/tables/sales-catalog/version
GET    /api/tables/sales-catalog/sync

GET    /api/orders?page=1&pageSize=20&status=&search=&date=
POST   /api/tables/{tableId}/orders/items
POST   /api/tables/{tableId}/orders/pay-and-close

GET    /api/cash-registers
POST   /api/cash-registers
GET    /api/cash-registers/shifts/current
POST   /api/cash-registers/shifts/open
POST   /api/cash-registers/shifts/close
GET    /api/cash-registers/shifts/{id}/summary

GET    /api/expenses?page=1&pageSize=20&startDate=&endDate=&supplierId=&category=
POST   /api/expenses
PUT    /api/expenses/{id}
DELETE /api/expenses/{id}

GET    /api/suppliers?page=1&pageSize=20&search=&includeInactive=false
POST   /api/suppliers
PUT    /api/suppliers/{id}
PATCH  /api/suppliers/{id}/status
DELETE /api/suppliers/{id}

GET    /api/statistics?period=current_month&includeTips=true

GET    /api/billing/receipts?page=1&pageSize=25&search=&fromDate=&toDate=
GET    /api/billing/orders?page=1&pageSize=25&search=&fromDate=&toDate=
GET    /api/billing/receipts/{id}

GET  /api/digital-menu/config
PUT  /api/digital-menu/parameters
PUT  /api/digital-menu/style
PUT  /api/digital-menu/items

GET    /api/images/{id}

GET/POST/PUT/DELETE /api/printing/agents[/...]
GET                   /api/printing/agents/discovered
POST                  /api/printing/agents/link-discovered
GET                   /api/printing/agents/{id}/available-printers
POST                  /api/printing/agents/{id}/discover-printers
GET/POST/PUT/DELETE /api/printing/printers[/...]
GET/POST/PUT/DELETE /api/printing/zones[/...]
GET                  /api/printing/routing-options
GET                  /api/printing/jobs
POST                 /api/printing/jobs/{id}/retry
POST                 /api/printing/jobs/{id}/reprint

POST /api/printing/agent/pair
POST /api/printing/agent/discovery
POST /api/printing/agent/discovery/{id}/poll
POST /api/printing/agent/discovery/{id}/acknowledge
POST /api/printing/agent/heartbeat
POST /api/printing/agent/printers/sync
GET  /api/printing/agent/jobs/pending
POST /api/printing/agent/jobs/{id}/status
HUB  /hubs/printing

GET    /api/settings/organization
PUT    /api/settings/organization
GET    /api/settings/access/permissions
GET    /api/settings/users
POST   /api/settings/users/invite

GET  /api/i18n/{language}
GET  /health
```

Los endpoints de tenants y `/users/me` requieren usuario y sesión válidos, pero funcionan sin tenant activo. `/api/users/me/info` y `/api/modules/available` requieren un token contextualizado con tenant y rol activos. Los futuros endpoints operativos pueden combinar `[RequireTenant]` y `[RequirePermission(PermissionCodes.X)]`.

## Impresión automática

El backend conserva la cola durable en PostgreSQL y SignalR solo avisa que hay trabajo disponible. Un agente Windows se vincula mediante un código de un solo uso; el servidor guarda únicamente hashes SHA-256 de sus credenciales. Cada agente puede atender varias zonas y cada zona varias impresoras. La ruta específica de producto prevalece sobre la ruta de categoría.

Desde la configuración se puede solicitar al agente que vuelva a consultar las colas instaladas en Windows. El backend envía la solicitud por el grupo SignalR autenticado del agente y conserva el resultado en `print_agent_discovered_printers`. Este inventario es independiente de `printers`: descubrir una cola nunca la configura ni le asigna permisos; el registro de configuración se crea únicamente cuando el administrador selecciona una opción y guarda el formulario.

Los ítems nuevos de una comanda y sus trabajos de impresión se guardan en la misma transacción de la base de datos de aplicación. Al instalarse sin credencial, el agente crea un secreto efímero, registra su disponibilidad por HTTP y consulta la autorización periódicamente. El estado temporal vive en `print_agent_discoveries`, por lo que cualquier réplica puede atender el registro, el listado administrativo o el polling. `GET /api/printing/agents/discovered` y el enlace posterior solo aceptan agentes cuya huella HMAC de red coincida con la del frontend administrador; la IP original se obtiene exclusivamente de `RemoteIpAddress` después de `ForwardedHeadersMiddleware` y solo se aceptan cabeceras reenviadas por proxies configurados como confiables. La interfaz actualiza activamente los equipos disponibles y únicamente un usuario con `printing.agents.manage` puede autorizar uno. La credencial se emite al agente que demuestra el secreto, se confirma después de guardarla con DPAPI y nunca se persiste en texto plano. El código de un solo uso continúa como respaldo. Una vez vinculado, consulta trabajos pendientes, mantiene una cola local SQLite idempotente y reporta los estados `PROCESSING`, `PRINTED` o `FAILED`. Reintentar reutiliza el trabajo fallido; reimprimir crea un trabajo auditado nuevo con referencia al original.

Configura `Printing:AgentDownloadUrl` con la URL del instalador publicado. `Printing:DiscoveryExpirationSeconds`, `Printing:DiscoveryPollIntervalSeconds` y, opcionalmente, `Printing:NetworkFingerprintKey` controlan el descubrimiento; si la clave dedicada no existe se usa la clave JWT como compatibilidad de despliegue. Mientras el agente continúa consultando, el backend renueva la vigencia del anuncio para que no desaparezca periódicamente; si el proceso deja de responder, el anuncio expira normalmente. Un agente habilitado que perdió su credencial vuelve a estar disponible para vinculación cuando supera el tiempo de detección offline. Los límites de heartbeat y detección offline se controlan con las demás opciones `Printing`. El servicio Windows y su instalación se documentan en `saviaup.print-agent/install.MD`; la arquitectura completa está en `saviaup.print-agent/ARCHITECTURE.md`.

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
      "code": "sales",
      "name": "Ventas",
      "order": 1,
      "isGrouped": false,
      "modules": [
        { "id": "...", "code": "tables", "name": "Mesas", "order": 1 }
      ],
      "options": []
    },
    {
      "code": "operation",
      "name": "Operación",
      "order": 2,
      "isGrouped": true,
      "modules": [
        { "id": "...", "code": "cash_registers", "name": "Cajas", "order": 1 },
        { "id": "...", "code": "orders", "name": "Comandas", "order": 2 },
        { "id": "...", "code": "statistics", "name": "Estadísticas", "order": 3 },
        { "id": "...", "code": "billing", "name": "Facturación", "order": 4 }
      ],
      "options": []
    },
    {
      "code": "inventory",
      "name": "Inventario",
      "order": 3,
      "isGrouped": true,
      "modules": [
        { "id": "...", "code": "products", "name": "Productos", "order": 1 },
        { "id": "...", "code": "categories", "name": "Categorías", "order": 2 },
        { "id": "...", "code": "inventory", "name": "Inventario", "order": 3 },
        { "id": "...", "code": "kitchen", "name": "Cocina", "order": 4 }
      ],
      "options": []
    },
    {
      "code": "expenses",
      "name": "Gastos",
      "order": 4,
      "isGrouped": true,
      "modules": [
        { "id": "...", "code": "expenses", "name": "Gastos", "order": 1 },
        { "id": "...", "code": "suppliers", "name": "Proveedores", "order": 2 }
      ],
      "options": []
    },
    {
      "code": "configuration",
      "name": "Configuración",
      "order": 5,
      "isGrouped": true,
      "modules": [
        { "id": "...", "code": "settings", "name": "Configuración", "order": 1 },
        { "id": "...", "code": "digital_menu", "name": "Menú digital", "order": 2 }
      ],
      "options": [
        { "code": "tables.manage", "moduleCode": "tables", "requiredPermissionCode": "tables.manage", "order": 3 },
        { "code": "cash-registers.manage", "moduleCode": "cash_registers", "requiredPermissionCode": "cash-registers.manage", "order": 4 }
      ]
    }
  ],
  "emptyStateMessage": null
}
```

Los nombres se localizan con `Accept-Language` (`es` o `en`, fallback español) y el frontend debe usar `code` como identificador estable. Secciones y módulos ya vienen ordenados. `isGrouped: false` indica que el único módulo debe mostrarse directamente, sin pestaña desplegable. Cuando el rol no tiene módulos u opciones, `sections` es `[]` y `emptyStateMessage` indica que debe contactar al administrador para gestionar los permisos.

Orden actual:

```text
1 Ventas: Mesas
2 Operación: Cajas, Comandas, Estadísticas, Facturación
3 Inventario: Productos, Categorías, Inventario, Cocina
4 Gastos: Gastos, Proveedores
5 Configuración: Configuración, Menú digital (con opciones directas para administrar salas/mesas y cajas)
```

La fuente de verdad es `Core/Navigation/NavigationCatalog.cs`. Cada módulo nuevo debe declarar sección/subcategoría y orden, además de seed, permisos, migración, copies y pruebas. El contrato incluye `options` para crecer con accesos administrativos respaldados por permisos `.manage`.

### Menú digital y permisos

El módulo `digital_menu` requiere `digital-menu.access` para aparecer en la navegación y consultar su configuración. Las mutaciones requieren también un permiso específico: `digital-menu.enable` para habilitar o deshabilitar el menú y administrar el slug, `digital-menu.style.manage` para estilo y plantillas, y `digital-menu.items.manage` para productos y categorías. Al asignar cualquiera de los permisos específicos a un rol, el permiso de acceso es obligatorio.

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

`GET /api/categories` devuelve solo activas para consumo de productos, inventarios y menús. La pantalla administrativa puede solicitar `includeInactive=true`. `PATCH /api/categories/{id}/status` recibe `{ "isActive": false }` y permite deshabilitar o reactivar. `DELETE` elimina físicamente la categoría solo si no tiene ingredientes ni productos; si está en uso devuelve `CATEGORY_IN_USE`.

## Productos y recetas

Los productos pertenecen al tenant activo y requieren una categoría activa del mismo tenant. `products.read` permite listar y `products.manage` permite crear, actualizar, activar/desactivar y eliminar. El listado usa paginación real y admite `search`, `categoryId`, `type=NORMAL|COMBO` e `includeInactive`.

Creación y actualización reciben:

```json
{
  "type": "NORMAL",
  "name": "Hamburguesa clásica",
  "categoryId": "00000000-0000-0000-0000-000000000000",
  "salePrice": 25000.0,
  "description": "Carne, queso y vegetales",
  "image": "data:image/webp;base64,...",
  "preparationTimeMinutes": 15,
  "isInventoryTracked": true,
  "recipe": [
    {
      "ingredientId": "11111111-1111-1111-1111-111111111111",
      "quantity": 150.0,
      "notes": "Carne molida de res",
      "order": 1
    },
    {
      "customIngredientName": "Pan brioche artesanal",
      "quantity": 1.0,
      "notes": "Panadería local",
      "order": 2
    }
  ]
}
```

`type` acepta `NORMAL` y `COMBO`; al omitirse usa `NORMAL`. Nombre y categoría son obligatorios. `salePrice` debe ser positivo para combos y productos normales sin variaciones. Cuando un producto normal incluye `variations`, cada variación define su propio precio y `salePrice` debe ser `null`; la migración normaliza de igual forma los productos existentes con variaciones. La imagen puede enviarse como data URL en base64; el backend la almacena en `stored_images` y la vincula de forma atómica mediante `ImageRef`.

### Composición y venta de combos

Un producto `COMBO` conserva los mismos datos comerciales, pero exige `comboGroups` con al menos un grupo y una opción. Cada grupo define `selectionType=SINGLE|MULTIPLE|FIXED`. Los grupos seleccionables usan `isRequired` y límites `minSelections`/`maxSelections`; un grupo `FIXED` es siempre obligatorio e incluye automáticamente todas sus opciones, sin recibir selecciones del cliente. Las opciones referencian productos `NORMAL` activos del mismo tenant y pueden fijar además una variación activa de ese producto mediante `productVariationId`; si el producto posee variaciones, elegir su opción base está prohibido y se debe indicar una variación activa. Cada opción indica `productQuantity` y un `priceAdjustment` opcional, positivo o negativo. Los combos no mantienen receta directa ni control de inventario propio; el consumo se deriva de la receta del producto elegido o incluido, también cuando se seleccionó una de sus variaciones.

```json
{
  "type": "COMBO",
  "name": "Combo almuerzo",
  "categoryId": "00000000-0000-0000-0000-000000000000",
  "salePrice": 32000,
  "comboGroups": [
    {
      "name": "Elige tu bebida",
      "selectionType": "SINGLE",
      "isRequired": true,
      "minSelections": 1,
      "maxSelections": 1,
      "options": [
        {
          "productId": "22222222-2222-2222-2222-222222222222",
          "productVariationId": "33333333-3333-3333-3333-333333333333",
          "productQuantity": 1,
          "priceAdjustment": 0
        }
      ]
    }
  ]
}
```

Al agregar el combo a una comanda, cada ítem envía `comboSelections: [{ comboGroupId, comboOptionId, quantity }]` únicamente para grupos seleccionables. `AddTableOrderItemsUseCase` vuelve a validar obligatoriedad, tipo, variación activa y límites, incorpora por sí mismo todas las opciones `FIXED`, calcula `UnitPrice = SalePrice + Σ(PriceAdjustment × quantity)` y guarda una instantánea en `order_item_combo_selections`. También construye `OrderItem.Notes` con los productos y variaciones seleccionados/fijos y las observaciones adicionales para operación e impresión. Las ediciones posteriores del combo no alteran la comanda existente. Un producto usado como opción responde `409 PRODUCT_IN_USE` al intentar desactivarlo, convertirlo en combo o eliminarlo; una variación usada tampoco se puede desactivar o eliminar hasta retirarla de las composiciones.

### Recetas y deducción automática de inventario

Cada producto puede tener una receta compuesta por ingredientes vinculados de inventario (`ingredientId`) o insumos personalizados (`customIngredientName`), con su cantidad requerida por porción/unidad vendida.

Al registrar el pago de una comanda (`POST /api/orders/table/{tableId}/checkout`), el backend ejecuta `PayAndCloseTableOrderUseCase`:
1. Identifica los productos efectivamente pagados en la transacción.
2. Consulta sus recetas completas en base de datos (`GetByIdsWithRecipesAsync`).
3. Calcula el consumo total de cada ingrediente vinculado (`recipeItem.Quantity * productQty`).
4. Genera movimientos automáticos de salida en inventario (`InventoryMovementCodes.Decrease`, motivo `Sale`) con nota de auditoría (ej. `Venta Mesa 1 - Orden #1024`) y actualiza el stock actual del ingrediente en tiempo real de forma transaccional.

Para combos, el paso 1 expande la instantánea seleccionada: `recipeItem.Quantity × productQuantity × selectionQuantity × comboQuantity`. El cobro parcial clona la selección al ítem pagado antes de descontar, por lo que cada unidad se procesa una sola vez.

## Inventario, ingredientes, movimientos y complementos

Las cuatro áreas viven bajo el módulo de Inventario y son completamente tenant-aware. Cada listado es paginado con `page` (desde 1), `pageSize` (1–100), `items`, `totalCount` y `totalPages`.

Un ingrediente requiere `categoryId`, `measurementUnitId` y `name`. `description` es opcional, `minimumStock` inicia en `0` y `initialStock` es opcional. Un stock inicial positivo genera un movimiento auditado `increase/initial`; después de crear, el stock solo cambia mediante movimientos.

Entradas válidas: `purchase`, `production`, `acquisition`. Salidas válidas: `expiration`, `loss`, `waste`. La API registra cantidad, stock anterior/posterior, nota opcional, usuario y fecha. Las salidas no pueden dejar stock negativo y responden `422 INVENTORY_INSUFFICIENT_STOCK` si exceden la existencia.

`GET /api/inventory` solo incluye ingredientes activos de categorías activas marcadas como inventariables. Informa `currentStock`, `minimumStock` e `isBelowMinimum`; admite `belowMinimum=true|false`.

Complementos está preparado para crecer por tipo. Actualmente expone unidades y cada tenant recibe de forma automática:

```text
gr  gramos
kg  kilogramos
und unidades
```

Los permisos son `inventory.stock.read`, `inventory.ingredients.read/manage`, `inventory.movements.read/manage` e `inventory.complements.read/manage`. No se pueden eliminar ingredientes con movimientos, unidades usadas ni categorías usadas; se pueden desactivar ingredientes/unidades con `PATCH /status`.

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

`Core.Tests` cubre autenticación, tenants, permisos, navegación, información contextual, categorías, productos, ingredientes, movimientos, stock y unidades. `IntegrationTests` arranca la API con EF InMemory y verifica los flujos HTTP de categorías y productos, además de paginado, defaults por tenant, stock inicial, entradas/salidas, stock insuficiente, restricciones de eliminación, middleware 401/403 y membership activa.

El frontend conectado está en `../saviaup.frontend`: desarrollo usa `useMockApi: false` y `apiUrl: http://localhost:5000`.

## Salas y mesas

`AddTableManagement` agrega salas ordenables, mesas con coordenadas 2D y el hook persistente de turnos de caja. `AddRestaurantTableShape` incorpora las formas `SQUARE`, `ROUND`, `RECTANGLE_HORIZONTAL` y `RECTANGLE_VERTICAL`, con `SQUARE` como valor por defecto para datos existentes. La configuración usa `/api/table-areas` y `/api/tables`; el snapshot operativo se obtiene en `/api/tables/operation`. `tables.read`, `tables.operate` y `tables.manage` separan consulta, operación y configuración.

La venta en mesas sincroniza su catálogo mediante un contrato independiente del listado administrativo: `/api/tables/sales-catalog/version` expone la versión vigente y `/api/tables/sales-catalog/sync` devuelve en una sola instantánea las categorías, productos con variaciones/recetas y salas/mesas activas del tenant. El parámetro interno `sales.catalog.lastModifiedAt` no se expone como ajuste editable; el backend lo crea o actualiza mediante EF al persistir cambios relevantes de categorías, productos, variaciones, salas o configuración de mesas, sin triggers de base de datos. Las transiciones operativas disponible/ocupada no invalidan el catálogo; activar o desactivar administrativamente una mesa sí lo hace.

`TablesHub` se publica en `/hubs/tables`, valida sesión/tenant/permiso y aísla cada conexión en un grupo por tenant. Emite `OnTableStatusChanged`, `OnTableOrderUpdated` y `OnTableSalesDataInvalidated` después de persistir los cambios correspondientes. Si `RequiresOpenCashRegister` está activo, las mutaciones se bloquean hasta que exista un turno sin fecha de cierre.

## Gastos y proveedores

El módulo de Gastos permite controlar egresos operativos de la organización:
- **Proveedores (`/api/suppliers`)**: administración de directorio de proveedores (nombre comercial, NIT/identificación, teléfono, email, contacto y estado activo/inactivo).
- **Gastos (`/api/expenses`)**: registro de gastos con concepto, categoría de gasto, monto, proveedor opcional, medio de pago, soporte documental y vinculación con el turno de caja vigente. El parámetro `expenses.lockFinancialFieldsAfterCreation`, activo por defecto en organizaciones nuevas y ante una configuración ausente o inválida, bloquea la edición posterior del valor, la fecha y el indicador de salida de caja. La organización puede desbloquearlos mediante la política dedicada de configuración; cambiarla exige `settings.expense-financial-fields.manage`.
- **Política de edición de gastos (`/api/settings/business/expense-editing-policy`)**: permite consultar el bloqueo y modificarlo con el permiso específico. `AddExpenseFinancialFieldsPolicyPermission` registra el permiso global sin habilitarlo en tenants ni asignarlo a roles existentes.
- **Integración con turnos de caja**: los egresos en efectivo registrados durante un turno abierto se consolidan en el resumen de cierre (`CashRegisterShiftSummaryDto`). El historial expone el fondo inicial y calcula el total en caja como recaudo de ventas + fondo inicial - gastos.
- **Métricas operativas**: el snapshot de mesas incluye `OpenShiftExpensesTotal` y `OpenShiftSalesTotal` para que el personal visualice en tiempo real la salud financiera del turno.

## Facturación y estadísticas

- **Facturación (`/api/billing`)**: consulta paginada de comprobantes emitidos (`/api/billing/receipts`) y órdenes completadas (`/api/billing/orders`), con búsqueda, filtros por rango de fechas y detalle para impresión/reimpresión de tirillas térmicas de 80mm.
- **Estadísticas (`/api/statistics`)**: agregación analítica de ventas y gastos por período (`current_month`, etc.), cálculo de KPIs (Ventas totales, Gastos totales, Ticket promedio, Propinas opcionales), desglose por medio de pago, ranking de productos más vendidos, ranking de meseros y comparativo agrupado Ventas vs Gastos.

## Almacenamiento optimizado de imágenes

Para evitar dependencias de almacenamiento externo en etapas tempranas y agilizar la respuesta del cliente:
- Se implementó la tabla `stored_images` con entidad `StoredImage`, que conserva el contenido binario comprimido en base64 junto con metadata técnica (mime type, peso, tenant).
- Entidades como `Category` y `Product` utilizan la clave foránea `ImageRef` hacia `stored_images`.
- Las consultas principales realizan una proyección en una única consulta SQL (evitando llamadas N+1 o endpoints adicionales), entregando la imagen lista para visualización inmediata en el frontend.
