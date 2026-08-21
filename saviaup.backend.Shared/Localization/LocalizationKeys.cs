namespace SaviaUp.Backend.Shared.Localization;

public static class LocalizationKeys
{
    public const string InvalidCredentials = "auth.invalidCredentials";
    public const string Unauthenticated = "auth.unauthenticated";
    public const string Forbidden = "auth.forbidden";
    public const string AccountDisabled = "auth.accountDisabled";
    public const string EmailAlreadyExists = "auth.emailAlreadyExists";
    public const string RefreshInvalid = "auth.refreshInvalid";
    public const string PasswordResetInvalid = "auth.passwordResetInvalid";
    public const string PasswordResetSent = "auth.passwordResetSent";
    public const string PasswordResetSubject = "auth.passwordResetSubject";
    public const string PasswordResetBody = "auth.passwordResetBody";
    public const string OrganizationInvitationSubject = "settings.users.invitationSubject";
    public const string OrganizationInvitationBody = "settings.users.invitationBody";
    public const string TenantRequired = "tenant.required";
    public const string TenantAccessDenied = "tenant.accessDenied";
    public const string TenantNotFound = "tenant.notFound";
    public const string NavigationNoModules = "navigation.noModules";
    public const string CategoryNotFound = "categories.notFound";
    public const string CategoryNameAlreadyExists = "categories.nameAlreadyExists";
    public const string CategoryInUse = "categories.inUse";
    public const string IngredientNotFound = "inventory.ingredients.notFound";
    public const string IngredientInUse = "inventory.ingredients.inUse";
    public const string MeasurementUnitNotFound = "inventory.units.notFound";
    public const string MeasurementUnitAlreadyExists = "inventory.units.alreadyExists";
    public const string MeasurementUnitInUse = "inventory.units.inUse";
    public const string InventoryMovementInvalid = "inventory.movements.invalid";
    public const string InventoryInsufficientStock = "inventory.movements.insufficientStock";
    public const string ProductNotFound = "products.notFound";
    public const string DiningAreaNotFound = "tables.areas.notFound";
    public const string DiningAreaAlreadyExists = "tables.areas.alreadyExists";
    public const string DiningAreaInUse = "tables.areas.inUse";
    public const string RestaurantTableNotFound = "tables.notFound";
    public const string RestaurantTableAlreadyExists = "tables.alreadyExists";
    public const string RestaurantTableOccupied = "tables.occupied";
    public const string CashRegisterClosed = "tables.cashRegisterClosed";
    public const string OrganizationDocumentOwnerOnly = "settings.organization.documentOwnerOnly";
    public const string OrganizationLogoInvalid = "settings.organization.logoInvalid";
    public const string PaymentMethodNotFound = "settings.payments.notFound";
    public const string PaymentMethodAlreadyExists = "settings.payments.alreadyExists";
    public const string SettingsRoleNotFound = "settings.roles.notFound";
    public const string SettingsRoleAlreadyExists = "settings.roles.alreadyExists";
    public const string SettingsRoleProtected = "settings.roles.protected";
    public const string SettingsRoleInUse = "settings.roles.inUse";
    public const string OrganizationUserNotFound = "settings.users.notFound";
    public const string OrganizationOwnerRequired = "settings.users.ownerRequired";
    public const string OrganizationInvitationExists = "settings.users.invitationExists";
    public const string PermissionNotEnabled = "settings.roles.permissionNotEnabled";
    public const string Validation = "common.validation";
    public const string InternalError = "common.internalError";

    public static string ModuleName(string moduleCode) => $"modules.{moduleCode}";
    public static string NavigationSectionName(string sectionCode) => $"navigation.sections.{sectionCode}";
    public static string NavigationOptionName(string optionCode) => $"navigation.options.{optionCode}";
}
