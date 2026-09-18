# Auditoría temporal de Savia Up

Esta auditoría se realizó sobre el código y el modelo EF; no se consultaron, alteraron ni se infirió el significado de datos de producción. La política resultante es: un evento real es un `DateTimeOffset` en UTC y `timestamptz`; un día comercial es `DateOnly` y `date`; una regla recurrente será `TimeOnly` y `time` cuando exista. La zona pertenece al tenant, se expresa con un identificador IANA y nunca depende del reloj ni de la configuración regional del servidor o del navegador.

## Inventario y clasificación

| Entidad | Propiedad | Tipo actual | Clasificación |
|---|---|---|---|
| CashRegister | CreatedAt | DateTimeOffset | Instante UTC |
| CashRegister | UpdatedAt | DateTimeOffset | Instante UTC |
| CashRegisterShift | OpenedAt | DateTimeOffset | Instante UTC |
| CashRegisterShift | ClosedAt | DateTimeOffset? | Instante UTC |
| Category | CreatedAt | DateTimeOffset | Instante UTC |
| Category | UpdatedAt | DateTimeOffset | Instante UTC |
| DiningArea | CreatedAt | DateTimeOffset | Instante UTC |
| DiningArea | UpdatedAt | DateTimeOffset | Instante UTC |
| Expense | ExpenseDate | DateTimeOffset | Fecha comercial heredada codificada como instante; requiere preservar original |
| Expense | AnnulledAt | DateTimeOffset? | Instante UTC |
| Expense | CreatedAt | DateTimeOffset | Instante UTC |
| Expense | UpdatedAt | DateTimeOffset | Instante UTC |
| Ingredient | CreatedAt | DateTimeOffset | Instante UTC |
| Ingredient | UpdatedAt | DateTimeOffset | Instante UTC |
| InventoryMovement | CreatedAt | DateTimeOffset | Instante UTC |
| MeasurementUnit | CreatedAt | DateTimeOffset | Instante UTC |
| MeasurementUnit | UpdatedAt | DateTimeOffset | Instante UTC |
| Order | PaidAt | DateTimeOffset? | Instante UTC |
| Order | CreatedAt | DateTimeOffset | Instante UTC |
| Order | UpdatedAt | DateTimeOffset | Instante UTC |
| OrderItem | CancelledAt | DateTimeOffset? | Instante UTC |
| OrderItem | CreatedAt | DateTimeOffset | Instante UTC |
| OrderItem | UpdatedAt | DateTimeOffset | Instante UTC |
| OrderReceipt | CreatedAt | DateTimeOffset | Instante UTC |
| OrganizationParameter | CreatedAt | DateTimeOffset | Instante UTC |
| OrganizationParameter | UpdatedAt | DateTimeOffset | Instante UTC |
| PasswordResetToken | ExpiresAt | DateTimeOffset | Instante UTC |
| PasswordResetToken | CreatedAt | DateTimeOffset | Instante UTC |
| PasswordResetToken | UsedAt | DateTimeOffset? | Instante UTC |
| PaymentMethod | CreatedAt | DateTimeOffset | Instante UTC |
| PaymentMethod | UpdatedAt | DateTimeOffset | Instante UTC |
| Product | CreatedAt | DateTimeOffset | Instante UTC |
| Product | UpdatedAt | DateTimeOffset | Instante UTC |
| ProductRecipeItem | CreatedAt | DateTimeOffset | Instante UTC |
| ProductRecipeItem | UpdatedAt | DateTimeOffset | Instante UTC |
| RefreshToken | ExpiresAt | DateTimeOffset | Instante UTC |
| RefreshToken | CreatedAt | DateTimeOffset | Instante UTC |
| RefreshToken | RevokedAt | DateTimeOffset? | Instante UTC |
| RestaurantTable | OccupiedAt | DateTimeOffset? | Instante UTC |
| RestaurantTable | CreatedAt | DateTimeOffset | Instante UTC |
| RestaurantTable | UpdatedAt | DateTimeOffset | Instante UTC |
| Role | CreatedAt | DateTimeOffset | Instante UTC |
| StoredImage | CreatedAt | DateTimeOffset | Instante UTC |
| StoredImage | UpdatedAt | DateTimeOffset | Instante UTC |
| Supplier | CreatedAt | DateTimeOffset | Instante UTC |
| Supplier | UpdatedAt | DateTimeOffset | Instante UTC |
| Tenant | CreatedAt | DateTimeOffset | Instante UTC |
| Tenant | UpdatedAt | DateTimeOffset | Instante UTC |
| TenantInvitation | CreatedAt | DateTimeOffset | Instante UTC |
| TenantInvitation | AcceptedAt | DateTimeOffset? | Instante UTC |
| TenantInvitation | RevokedAt | DateTimeOffset? | Instante UTC |
| TenantMembership | DisabledUntil | DateTimeOffset? | Instante UTC |
| TenantMembership | CreatedAt | DateTimeOffset | Instante UTC |
| User | CreatedAt | DateTimeOffset | Instante UTC |
| User | UpdatedAt | DateTimeOffset | Instante UTC |

`Expense.BusinessDate` es el único día comercial añadido: conserva el día elegido para el gasto sin convertirlo en un instante. `Expense.ExpenseDate` no se reinterpreta de forma retroactiva. No existen campos `InvoiceDate`, `AccountingDate` ni horarios recurrentes en el modelo actual; no se inventaron campos para eventos de cocina que tampoco existen. Los minutos de preparación de producto son una duración, no una hora del día.

## Cambios aplicados

- **Domain:** `Tenant.TimeZoneId`, por defecto `America/Bogota`; puertos `ITimeZoneService` e `IOrganizationTimeZone`; DTO de tenant, sesión, organización y configuración con `timeZoneId`; `Expense.BusinessDate` y `disabledThroughDate` como fechas de negocio.
- **Core:** NodaTime se encapsula detrás de los puertos. La conversión de una hora local ambigua o inexistente se rechaza; los inicios de día y rangos sí resuelven correctamente transiciones DST. Los casos de uso reciben el reloj mediante `IDateTimeProvider`.
- **Infrastructure/PostgreSQL:** `IanaTimeZoneService` usa exclusivamente la base TZDB de IANA; los instantes se mantienen como `timestamp with time zone`; el nuevo día comercial usa `date`. Se agregaron índices `(TenantId, CreatedAt)`, `(TenantId, PaidAt)`, `(TenantId, BusinessDate)` y `(TenantId, CreatedAt)` de recibos para rangos sargables.
- **Endpoints:** `GET /api/orders`, `/api/orders/items`, `/api/billing/receipts`, `/api/billing/orders` y `/api/expenses` reciben `fromDate` y `toDate` como `yyyy-MM-dd`. El backend los convierte al rango UTC de la organización con `>= inicio` y `< fin`. Los instantes de respuestas siguen siendo ISO 8601 UTC. Enviar una fecha-hora a esos filtros se rechaza.
- **Reportes:** estadísticas y cierre de caja agrupan ventas por `PaidAt` en el día local; los gastos usan `BusinessDate` cuando existe. El cálculo de una caja conserva su intervalo de instantes, por lo que una apertura y cierre a ambos lados de medianoche o DST no cambia la duración.
- **Frontend:** el contexto de organización transporta `timeZoneId`; `OrganizationTime` y `organizationDate` muestran instantes en esa zona aun si el dispositivo está en otra. Los filtros transmiten el día `yyyy-MM-dd` sin fabricar `23:59:59` ni aplicar la zona del dispositivo.

## Migración segura

Las migraciones son aditivas y no actualizan datos históricos:

1. `AddOrganizationTimeZone` agrega `tenants.TimeZoneId` no nulo con default `America/Bogota`, por lo que las organizaciones existentes tienen una configuración explícita.
2. `AddExpenseBusinessDate` agrega una columna nullable para no inventar el día comercial de gastos existentes.
3. `AddTemporalReportIndexes` agrega los índices requeridos para las consultas por rango.

Los scripts idempotentes generados, listos para ser revisados y ejecutados por el proceso de despliegue, son [organization-time-zone.sql](migrations/organization-time-zone.sql) y [temporal-schema.sql](migrations/temporal-schema.sql). Aplicar primero la base Platform y luego la Application. Ningún script convierte timestamps históricos.

Antes de decidir una migración histórica, ejecutar estos controles de solo lectura contra una copia de producción y registrar el resultado por tenant:

```sql
-- Tipos físicos que debe confirmar el operador.
SELECT table_name, column_name, data_type
FROM information_schema.columns
WHERE table_schema = 'public'
  AND column_name IN ('CreatedAt', 'UpdatedAt', 'PaidAt', 'OpenedAt', 'ClosedAt', 'ExpenseDate');

-- Ventas pagadas sin instante de pago: no pueden entrar en reportes basados en venta.
SELECT "TenantId", count(*)
FROM orders
WHERE "Status" = 'PAID' AND "PaidAt" IS NULL
GROUP BY "TenantId";

-- Gastos históricos sin día comercial explícito: requieren decisión del negocio antes de backfill.
SELECT "TenantId", count(*)
FROM expenses
WHERE "BusinessDate" IS NULL
GROUP BY "TenantId";
```

Si se confirma documentalmente que una columna `timestamp without time zone` histórica expresa la hora local de un tenant, se debe crear una migración separada, ensayada en copia, que use la zona IANA de ese tenant y deje evidencia de la decisión. No es seguro asumir Colombia para registros de otros países ni convertir con un offset fijo.

## Riesgos y cobertura

Se encontraron límites UTC en facturación, estadísticas y operación; una métrica diaria que podía abarcar tres días y limitarse a 100 órdenes; agrupaciones por día/mes UTC; filtros de gastos que mezclaban `ExpenseDate` y `CreatedAt`; y el frontend que usaba la zona del dispositivo y finales `23:59:59`.

Las pruebas nuevas cubren Bogotá, Nueva York en entrada y salida DST, Madrid, día omitido de Apia, medianoche, rangos inclusivos/exclusivos, usuario en otra zona, duración durante DST, gastos con fecha comercial y SQL generado sin `date_trunc`. Aún hace falta validar los hallazgos anteriores contra los datos reales antes de cualquier backfill, y los informes de facturación continúan usando su instante técnico de creación porque el modelo no contiene una fecha fiscal separada.
