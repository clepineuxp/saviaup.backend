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

## Inmutabilidad de gastos

`POST /api/expenses` define el valor, la fecha de negocio y si el dinero sale de caja. Esos campos dejan de formar parte de `UpdateExpenseRequest`; `PUT /api/expenses/{expenseId}` solo actualiza nombre, descripción, medio de pago y proveedor. `UpdateExpenseUseCase` reutiliza los valores financieros persistidos durante la validación y no los reasigna, incluso si un cliente intenta enviar propiedades adicionales.
