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
