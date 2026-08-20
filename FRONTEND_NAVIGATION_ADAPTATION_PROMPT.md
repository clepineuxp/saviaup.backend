# Prompt de adaptación para `saviaup.frontend`

```text
Adapta `saviaup.frontend` al contrato ordenado y agrupable de navegación de Savia Up.

Alcance:
- Modifica únicamente `saviaup.frontend`.
- Lee primero su `AGENTS.md` y respeta la arquitectura existente.
- No modifiques `saviaup.backend` ni hagas commit/push salvo solicitud expresa.
- Consume la API real configurada, sin sustituir el flujo por mocks.

Después del login con tenant activo, o después de crear/seleccionar una organización, persiste primero los tokens contextualizados y carga en paralelo:
- GET `/api/users/me/info`.
- GET `/api/modules/available`.

No llames estos endpoints mientras `requiresTenantSelection` sea `true`.

El contrato de `GET /api/modules/available` es:

{
  "sections": [
    {
      "code": "operation",
      "name": "Operación",
      "order": 2,
      "isGrouped": true,
      "modules": [
        { "id": "guid", "code": "orders", "name": "Pedidos", "order": 1 },
        { "id": "guid", "code": "reports", "name": "Reportes", "order": 2 },
        { "id": "guid", "code": "billing", "name": "Facturación", "order": 3 }
      ],
      "options": []
    }
  ],
  "emptyStateMessage": null
}

Una opción administrativa futura tendrá:

{
  "code": "products.manage",
  "moduleCode": "products",
  "name": "Administrar productos",
  "order": 2
}

Reglas obligatorias:
1. Usa `section.order` y el `order` interno como fuente de verdad; no ordenes alfabéticamente.
2. Con `isGrouped: false`, renderiza el único módulo/opción directamente, sin pestaña o dropdown contenedor.
3. Con `isGrouped: true`, crea un grupo con `section.name` y apila sus módulos/opciones en el orden recibido.
4. Usa los nombres localizados de la API. No hardcodees nombres de secciones o módulos.
5. Usa `code` como identificador estable y mantenlo separado de la ruta y el icono.
6. No infieras permisos ni recalcules `isGrouped`; el backend ya filtra para el rol actual.
7. Si `sections` es `[]`, muestra exactamente `emptyStateMessage` y conserva acciones generales como logout o cambio de organización.
8. Prepara modelos y componentes para que `options` pueda contener accesos `.manage` sin cambiar el contrato.
9. Maneja códigos desconocidos con un fallback seguro sin romper toda la navegación.

Configuración actual:
- Orden 1, `sales`: `tables` (1), acceso directo.
- Orden 2, `operation`: `orders` (1), `reports` (2), `billing` (3), grupo.
- Orden 3, `inventory`: `products` (1), `categories` (2), `inventory` (3), `kitchen` (4), grupo.
- Orden 4, `configuration`: `settings` (1), acceso directo mientras no tenga más elementos.

Agrega `categories` al mapa frontend code → ruta/icono. Si su pantalla todavía no existe, sigue el patrón actual de placeholder o ruta pendiente sin inventar permisos.

Modelos mínimos:
- AvailableModulesResponse: `sections`, `emptyStateMessage`.
- NavigationSection: `code`, `name`, `order`, `isGrouped`, `modules`, `options`.
- AvailableModule: `id`, `code`, `name`, `order`.
- NavigationOption: `code`, `moduleCode`, `name`, `order`.
- UserInfo y DTO relacionados para `/api/users/me/info`.

Estado y sesión:
- Muestra loading/skeleton hasta completar user info y navegación y evita flashes no autorizados.
- Limpia navegación y user info al cerrar sesión.
- Al cambiar de organización, descarta el contexto anterior y vuelve a cargar ambos endpoints.
- Respeta el interceptor actual para JWT y `Accept-Language`.
- Un `403 TENANT_REQUIRED` debe ir al selector de organización.
- Un `200` con `sections: []` es estado vacío, no error.
- Conserva reintento para errores recuperables.

Pruebas requeridas:
- Deserialización del contrato y orden de secciones/módulos.
- Acceso directo para `isGrouped: false` y agrupamiento para `true`.
- Ruta/icono de `categories`.
- Nombres provenientes de la API.
- Estado vacío, opciones futuras, logout y cambio de tenant.
- Ausencia de peticiones antes de seleccionar tenant.
- Manejo de 401, `403 TENANT_REQUIRED`, error recuperable y reintento.

Actualiza `AGENTS.md` y `README.md` del frontend con el bootstrap, el contrato y el mapa code → ruta/icono. Ejecuta build, lint, formatter y pruebas. Reporta archivos modificados, decisiones y resultados; no declares éxito si alguna validación falla.
```
