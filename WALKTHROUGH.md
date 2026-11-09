# Walkthrough — Savia Up Backend

## Control de versiones

No crear commits ni hacer push bajo ninguna circunstancia, salvo que el usuario lo solicite explícitamente en el mensaje actual.

## Product combos

La feature extiende `Product` sin cambiar el flujo de productos `NORMAL`:

- `product_combo_groups` modela grupos únicos, múltiples o fijos. Los fijos incluyen todas sus opciones automáticamente y no aceptan selección del cliente.
- `product_combo_options` relaciona cada grupo con productos normales del tenant, cantidad incluida y ajuste de precio.
- `order_item_combo_selections` conserva la selección exacta de la venta como snapshot.
- crear y editar un combo exige al menos un grupo y una opción; no se permiten autorreferencias ni combos anidados.
- al agregar a la comanda, el backend valida identificadores y límites y recalcula el precio.
- las observaciones del ítem combinan la composición validada —seleccionada y fija— con la nota adicional del usuario.
- al cobrar, las selecciones se expanden a productos normales y sus recetas descuentan inventario con todos los multiplicadores.

La migración es `AddProductComboComposition`. Las pruebas relevantes viven en `ProductUseCaseTests`, `OrderUseCaseTests` y el flujo HTTP de `CriticalEndpointsTests`.

## Política de edición financiera de gastos

`POST /api/expenses` define el valor, la fecha de negocio y si el dinero sale de caja. El parámetro de organización `expenses.lockFinancialFieldsAfterCreation` controla si `PUT /api/expenses/{expenseId}` puede modificar esos campos: su valor inicial es `true` para organizaciones nuevas y una clave ausente o inválida también se interpreta como bloqueada. Cuando está activo, Core conserva `Amount`, `ExpenseDate`, `BusinessDate` e `IsCashOut`; cuando está desactivado, el contrato acepta sus nuevos valores y vuelve a validar fecha, monto y turno de caja.

La política se consulta en `GET /api/settings/business/expense-editing-policy` y solo puede cambiarse mediante `PUT /api/settings/business/expense-editing-policy` con `settings.expense-financial-fields.manage`. La migración `AddExpenseFinancialFieldsPolicyPermission` inserta únicamente este permiso global: no habilita el permiso en organizaciones ni lo asigna a roles existentes.

## Total de turnos de caja

`CashRegisterShiftDto` y `CashRegisterShiftSummaryDto` exponen `InitialOpeningAmount` y `TotalInCashAmount`. El primero suma los saldos registrados durante la apertura; el segundo aplica `TotalCollectedAmount + InitialOpeningAmount - TotalExpensesAmount`, de modo que el historial y el detalle usan la misma cifra.

## Reportes y exportación asíncrona a Excel

La feature de reportes permite configurar, generar y descargar informes analíticos en formato Excel (`.xlsx`) sin bloquear peticiones HTTP:

- **Entidad `GeneratedReport`**: modela cada solicitud de informe con aislamiento multi-tenant (`TenantId`), estado (`PENDING`, `PROCESSING`, `COMPLETED`, `FAILED`), parámetros en JSON, ruta de archivo, tamaño y auditoría de usuario.
- **Migraciones EF Core**:
  - `20261109034921_AddGeneratedReports` (`ApplicationDbContext`): crea la tabla `generated_reports` con índices por `TenantId + CreatedAt`, `TenantId + ReportType` y `TenantId + Status`.
  - `20261109035039_AddReportsModuleToSeed` (`PlatformDbContext`): registra el módulo 16 `reports` y los permisos `reports.read` y `reports.manage`.
- **Motor Excel ClosedXML (`ClosedXmlReportGenerator`)**:
  - *Estadísticas*: KPIs consolidados y tablas personalizables por productos top, mesas, meseros y categorías.
  - *Gastos*: detalle de egresos con filtro de salida de caja (`ALL`, `ONLY_CASH_OUT`, `NO_CASH_OUT`).
  - *Ventas / Pedidos*: agrupado por *comandas* (mesa, orden, cantidad de ítems y total) o por *productos* (productos individuales vendidos).
  - *Ventas y Gastos*: ingresos vs egresos indicando fecha, hora y usuario responsable.
  - *Cajas*: turnos con apertura, ventas, gastos, balance y recaudación por cada medio de pago.
- **Procesamiento asíncrono**: `ReportJobQueue` encola el trabajo en memoria mediante canales (`System.Threading.Channels`) y `ReportGenerationBackgroundService` procesa la generación en segundo plano.
- **Almacenamiento persistente**: los archivos generados se guardan en el volumen PVC bajo `{tenantId}/informes/{guid}_{fileName}` mediante `LocalFileStorage`.
- **Endpoints en `ReportsController`**:
  - `POST /api/reports`: acepta la solicitud y responde `202 Accepted` con el DTO del reporte en estado `PENDING`.
  - `GET /api/reports`: lista los informes recientes del tenant.
  - `GET /api/reports/{id}`: consulta el estado del reporte.
  - `GET /api/reports/{id}/download`: descarga el archivo Excel.
- **Agrupación por períodos (`PeriodGrouping`)**:
  - Soporta `NONE`, `WEEKLY` (semanas lunes a domingo) y `MONTHLY` (mes calendario).
  - Incluye hojas dedicadas de evolución periódica y columna `Período` en los reportes de ventas, gastos, consolidado y turnos de caja.
  - Normalización de tipos de reporte para compatibilidad completa con identificadores del frontend (`SALES`, `ORDERS`, `SALES_AND_EXPENSES`, `SALES_EXPENSES`, `STATISTICS`, `EXPENSES`, `CASH_REGISTERS`).

