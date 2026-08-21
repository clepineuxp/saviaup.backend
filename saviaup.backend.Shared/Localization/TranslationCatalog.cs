using System.Collections.ObjectModel;

namespace SaviaUp.Backend.Shared.Localization;

public static class TranslationCatalog
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Languages =
        new ReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>(
            new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["es"] = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
                {
                    ["auth.login.title"] = "Iniciar sesión",
                    ["auth.login.email"] = "Correo electrónico",
                    ["auth.register.title"] = "Crear cuenta",
                    [LocalizationKeys.InvalidCredentials] = "Credenciales inválidas.",
                    [LocalizationKeys.Unauthenticated] = "Debes iniciar sesión para continuar.",
                    [LocalizationKeys.Forbidden] = "No tienes permisos para realizar esta acción.",
                    [LocalizationKeys.AccountDisabled] = "La cuenta no está disponible.",
                    [LocalizationKeys.EmailAlreadyExists] = "Ya existe una cuenta con ese correo.",
                    [LocalizationKeys.RefreshInvalid] = "La sesión ya no es válida.",
                    [LocalizationKeys.PasswordResetInvalid] = "El enlace de recuperación no es válido o expiró.",
                    [LocalizationKeys.PasswordResetSent] = "Si existe una cuenta asociada al correo, recibirás instrucciones para recuperar tu contraseña.",
                    [LocalizationKeys.PasswordResetSubject] = "Recupera tu contraseña de Savia Up",
                    [LocalizationKeys.PasswordResetBody] = "Usa el siguiente enlace para crear una nueva contraseña: {0}",
                    [LocalizationKeys.OrganizationInvitationSubject] = "Te invitaron a una organización en Savia Up",
                    [LocalizationKeys.OrganizationInvitationBody] = "Te invitaron a unirte a {0}. Ingresa o crea tu cuenta con este correo: {1}",
                    [LocalizationKeys.TenantRequired] = "Selecciona una organización para continuar.",
                    [LocalizationKeys.TenantAccessDenied] = "No tienes acceso a esta organización.",
                    [LocalizationKeys.TenantNotFound] = "No encontramos la organización solicitada.",
                    [LocalizationKeys.NavigationNoModules] = "No tienes módulos disponibles. Habla con el administrador de tu organización para gestionar tus permisos.",
                    [LocalizationKeys.NavigationSectionName("sales")] = "Ventas",
                    [LocalizationKeys.NavigationSectionName("operation")] = "Operación",
                    [LocalizationKeys.NavigationSectionName("inventory")] = "Inventario",
                    [LocalizationKeys.NavigationSectionName("configuration")] = "Configuración",
                    [LocalizationKeys.ModuleName("orders")] = "Pedidos",
                    [LocalizationKeys.ModuleName("tables")] = "Mesas",
                    [LocalizationKeys.ModuleName("inventory")] = "Inventario",
                    [LocalizationKeys.ModuleName("products")] = "Productos",
                    [LocalizationKeys.ModuleName("categories")] = "Categorías",
                    [LocalizationKeys.ModuleName("kitchen")] = "Cocina",
                    [LocalizationKeys.ModuleName("reports")] = "Reportes",
                    [LocalizationKeys.ModuleName("billing")] = "Facturación",
                    [LocalizationKeys.ModuleName("settings")] = "Configuración",
                    [LocalizationKeys.NavigationOptionName("tables.manage")] = "Administrar mesas",
                    [LocalizationKeys.CategoryNotFound] = "No encontramos la categoría solicitada.",
                    [LocalizationKeys.CategoryNameAlreadyExists] = "Ya existe una categoría con ese nombre en la organización.",
                    [LocalizationKeys.CategoryInUse] = "La categoría tiene ingredientes asociados. Deshabilítala en lugar de eliminarla.",
                    [LocalizationKeys.IngredientNotFound] = "No encontramos el ingrediente solicitado.",
                    [LocalizationKeys.IngredientInUse] = "El ingrediente tiene movimientos de inventario. Deshabilítalo en lugar de eliminarlo.",
                    [LocalizationKeys.MeasurementUnitNotFound] = "No encontramos la unidad de medida solicitada.",
                    [LocalizationKeys.MeasurementUnitAlreadyExists] = "Ya existe una unidad con ese código o nombre en la organización.",
                    [LocalizationKeys.MeasurementUnitInUse] = "La unidad tiene ingredientes asociados y no puede eliminarse.",
                    [LocalizationKeys.InventoryMovementInvalid] = "El tipo, motivo o cantidad del movimiento de inventario no es válido.",
                    [LocalizationKeys.InventoryInsufficientStock] = "El ingrediente no tiene existencias suficientes para realizar la salida.",
                    [LocalizationKeys.ProductNotFound] = "No encontramos el producto solicitado.",
                    [LocalizationKeys.DiningAreaNotFound] = "No encontramos la sala solicitada.",
                    [LocalizationKeys.DiningAreaAlreadyExists] = "Ya existe una sala con ese nombre u orden en la organización.",
                    [LocalizationKeys.DiningAreaInUse] = "La sala tiene mesas asociadas. Elimina o reasigna las mesas antes de borrarla.",
                    [LocalizationKeys.RestaurantTableNotFound] = "No encontramos la mesa solicitada.",
                    [LocalizationKeys.RestaurantTableAlreadyExists] = "Ya existe una mesa con ese nombre en la sala.",
                    [LocalizationKeys.RestaurantTableOccupied] = "La mesa tiene una orden activa y no puede eliminarse.",
                    [LocalizationKeys.CashRegisterClosed] = "Debes abrir un turno de caja para operar las mesas.",
                    [LocalizationKeys.OrganizationDocumentOwnerOnly] = "Solo el propietario de la organización puede modificar el documento.",
                    [LocalizationKeys.OrganizationLogoInvalid] = "El logo debe ser PNG, JPEG o WebP y no superar 2 MB.",
                    [LocalizationKeys.PaymentMethodNotFound] = "No encontramos el medio de pago solicitado.",
                    [LocalizationKeys.PaymentMethodAlreadyExists] = "Ya existe un medio de pago con ese nombre en la organización.",
                    [LocalizationKeys.SettingsRoleNotFound] = "No encontramos el rol solicitado.",
                    [LocalizationKeys.SettingsRoleAlreadyExists] = "Ya existe un rol con ese nombre en la organización.",
                    [LocalizationKeys.SettingsRoleProtected] = "El rol propietario del sistema no se puede modificar ni eliminar.",
                    [LocalizationKeys.SettingsRoleInUse] = "El rol está asignado a usuarios y no puede eliminarse.",
                    [LocalizationKeys.OrganizationUserNotFound] = "No encontramos el usuario o la invitación solicitada.",
                    [LocalizationKeys.OrganizationOwnerRequired] = "La organización debe conservar al menos un propietario activo.",
                    [LocalizationKeys.OrganizationInvitationExists] = "El usuario ya pertenece a la organización o tiene una invitación pendiente.",
                    [LocalizationKeys.PermissionNotEnabled] = "Uno o más permisos no están habilitados para esta organización.",
                    [LocalizationKeys.Validation] = "Revisa los datos enviados.",
                    [LocalizationKeys.InternalError] = "Ocurrió un error inesperado."
                }),
                ["en"] = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
                {
                    ["auth.login.title"] = "Sign in",
                    ["auth.login.email"] = "Email address",
                    ["auth.register.title"] = "Create account",
                    [LocalizationKeys.InvalidCredentials] = "Invalid credentials.",
                    [LocalizationKeys.Unauthenticated] = "You must sign in to continue.",
                    [LocalizationKeys.Forbidden] = "You don't have permission to perform this action.",
                    [LocalizationKeys.AccountDisabled] = "The account is unavailable.",
                    [LocalizationKeys.EmailAlreadyExists] = "An account with that email already exists.",
                    [LocalizationKeys.RefreshInvalid] = "The session is no longer valid.",
                    [LocalizationKeys.PasswordResetInvalid] = "The recovery link is invalid or expired.",
                    [LocalizationKeys.PasswordResetSent] = "If an account exists for that email, you will receive password recovery instructions.",
                    [LocalizationKeys.PasswordResetSubject] = "Reset your Savia Up password",
                    [LocalizationKeys.PasswordResetBody] = "Use this link to create a new password: {0}",
                    [LocalizationKeys.OrganizationInvitationSubject] = "You were invited to an organization in Savia Up",
                    [LocalizationKeys.OrganizationInvitationBody] = "You were invited to join {0}. Sign in or create your account with this email: {1}",
                    [LocalizationKeys.TenantRequired] = "Select an organization to continue.",
                    [LocalizationKeys.TenantAccessDenied] = "You don't have access to this organization.",
                    [LocalizationKeys.TenantNotFound] = "The requested organization was not found.",
                    [LocalizationKeys.NavigationNoModules] = "You don't have any available modules. Contact your organization administrator to manage your permissions.",
                    [LocalizationKeys.NavigationSectionName("sales")] = "Sales",
                    [LocalizationKeys.NavigationSectionName("operation")] = "Operations",
                    [LocalizationKeys.NavigationSectionName("inventory")] = "Inventory",
                    [LocalizationKeys.NavigationSectionName("configuration")] = "Configuration",
                    [LocalizationKeys.ModuleName("orders")] = "Orders",
                    [LocalizationKeys.ModuleName("tables")] = "Tables",
                    [LocalizationKeys.ModuleName("inventory")] = "Inventory",
                    [LocalizationKeys.ModuleName("products")] = "Products",
                    [LocalizationKeys.ModuleName("categories")] = "Categories",
                    [LocalizationKeys.ModuleName("kitchen")] = "Kitchen",
                    [LocalizationKeys.ModuleName("reports")] = "Reports",
                    [LocalizationKeys.ModuleName("billing")] = "Billing",
                    [LocalizationKeys.ModuleName("settings")] = "Settings",
                    [LocalizationKeys.NavigationOptionName("tables.manage")] = "Manage tables",
                    [LocalizationKeys.CategoryNotFound] = "The requested category was not found.",
                    [LocalizationKeys.CategoryNameAlreadyExists] = "A category with that name already exists in the organization.",
                    [LocalizationKeys.CategoryInUse] = "The category has associated ingredients. Disable it instead of deleting it.",
                    [LocalizationKeys.IngredientNotFound] = "The requested ingredient was not found.",
                    [LocalizationKeys.IngredientInUse] = "The ingredient has inventory movements. Disable it instead of deleting it.",
                    [LocalizationKeys.MeasurementUnitNotFound] = "The requested measurement unit was not found.",
                    [LocalizationKeys.MeasurementUnitAlreadyExists] = "A unit with that code or name already exists in the organization.",
                    [LocalizationKeys.MeasurementUnitInUse] = "The unit has associated ingredients and cannot be deleted.",
                    [LocalizationKeys.InventoryMovementInvalid] = "The inventory movement type, reason, or quantity is invalid.",
                    [LocalizationKeys.InventoryInsufficientStock] = "The ingredient does not have enough stock for this decrease.",
                    [LocalizationKeys.ProductNotFound] = "The requested product was not found.",
                    [LocalizationKeys.DiningAreaNotFound] = "The requested dining area was not found.",
                    [LocalizationKeys.DiningAreaAlreadyExists] = "A dining area with that name or order already exists in the organization.",
                    [LocalizationKeys.DiningAreaInUse] = "The dining area has tables. Delete or move them before deleting it.",
                    [LocalizationKeys.RestaurantTableNotFound] = "The requested table was not found.",
                    [LocalizationKeys.RestaurantTableAlreadyExists] = "A table with that name already exists in the dining area.",
                    [LocalizationKeys.RestaurantTableOccupied] = "The table has an active order and cannot be deleted.",
                    [LocalizationKeys.CashRegisterClosed] = "Open a cash register shift before operating tables.",
                    [LocalizationKeys.OrganizationDocumentOwnerOnly] = "Only the organization owner can change its document.",
                    [LocalizationKeys.OrganizationLogoInvalid] = "The logo must be PNG, JPEG or WebP and no larger than 2 MB.",
                    [LocalizationKeys.PaymentMethodNotFound] = "The requested payment method was not found.",
                    [LocalizationKeys.PaymentMethodAlreadyExists] = "A payment method with that name already exists in the organization.",
                    [LocalizationKeys.SettingsRoleNotFound] = "The requested role was not found.",
                    [LocalizationKeys.SettingsRoleAlreadyExists] = "A role with that name already exists in the organization.",
                    [LocalizationKeys.SettingsRoleProtected] = "The system owner role cannot be changed or deleted.",
                    [LocalizationKeys.SettingsRoleInUse] = "The role is assigned to users and cannot be deleted.",
                    [LocalizationKeys.OrganizationUserNotFound] = "The requested user or invitation was not found.",
                    [LocalizationKeys.OrganizationOwnerRequired] = "The organization must keep at least one active owner.",
                    [LocalizationKeys.OrganizationInvitationExists] = "The user already belongs to the organization or has a pending invitation.",
                    [LocalizationKeys.PermissionNotEnabled] = "One or more permissions are not enabled for this organization.",
                    [LocalizationKeys.Validation] = "Review the submitted data.",
                    [LocalizationKeys.InternalError] = "An unexpected error occurred."
                })
            });

    public static IReadOnlyDictionary<string, string> Get(string? language)
        => Languages.TryGetValue(Normalize(language), out var translations) ? translations : Languages["es"];

    public static string Translate(string key, string? language)
        => Get(language).TryGetValue(key, out var value) ? value : key;

    public static string Normalize(string? language)
    {
        if (string.IsNullOrWhiteSpace(language)) return "es";
        var code = language.Split(',', ';', '-')[0].Trim().ToLowerInvariant();
        return Languages.ContainsKey(code) ? code : "es";
    }
}
