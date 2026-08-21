namespace SaviaUp.Backend.Shared.Constants;

public static class ErrorCodes
{
    public const string InvalidCredentials = "AUTH_INVALID_CREDENTIALS";
    public const string Unauthenticated = "AUTH_UNAUTHENTICATED";
    public const string Forbidden = "AUTH_FORBIDDEN";
    public const string RefreshInvalid = "AUTH_REFRESH_INVALID";
    public const string RefreshExpired = "AUTH_REFRESH_EXPIRED";
    public const string AccountDisabled = "AUTH_ACCOUNT_DISABLED";
    public const string EmailAlreadyExists = "AUTH_EMAIL_ALREADY_EXISTS";
    public const string PasswordResetInvalid = "AUTH_PASSWORD_RESET_INVALID";
    public const string TenantRequired = "TENANT_REQUIRED";
    public const string TenantNotFound = "TENANT_NOT_FOUND";
    public const string TenantAccessDenied = "TENANT_ACCESS_DENIED";
    public const string CategoryNotFound = "CATEGORY_NOT_FOUND";
    public const string CategoryNameAlreadyExists = "CATEGORY_NAME_ALREADY_EXISTS";
    public const string CategoryInUse = "CATEGORY_IN_USE";
    public const string IngredientNotFound = "INGREDIENT_NOT_FOUND";
    public const string IngredientInUse = "INGREDIENT_IN_USE";
    public const string MeasurementUnitNotFound = "MEASUREMENT_UNIT_NOT_FOUND";
    public const string MeasurementUnitAlreadyExists = "MEASUREMENT_UNIT_ALREADY_EXISTS";
    public const string MeasurementUnitInUse = "MEASUREMENT_UNIT_IN_USE";
    public const string InventoryMovementInvalid = "INVENTORY_MOVEMENT_INVALID";
    public const string InventoryInsufficientStock = "INVENTORY_INSUFFICIENT_STOCK";
    public const string ProductNotFound = "PRODUCT_NOT_FOUND";
    public const string DiningAreaNotFound = "DINING_AREA_NOT_FOUND";
    public const string DiningAreaAlreadyExists = "DINING_AREA_ALREADY_EXISTS";
    public const string DiningAreaInUse = "DINING_AREA_IN_USE";
    public const string RestaurantTableNotFound = "RESTAURANT_TABLE_NOT_FOUND";
    public const string RestaurantTableAlreadyExists = "RESTAURANT_TABLE_ALREADY_EXISTS";
    public const string RestaurantTableOccupied = "RESTAURANT_TABLE_OCCUPIED";
    public const string CashRegisterClosed = "CASH_REGISTER_CLOSED";
    public const string OrganizationDocumentOwnerOnly = "ORGANIZATION_DOCUMENT_OWNER_ONLY";
    public const string OrganizationLogoInvalid = "ORGANIZATION_LOGO_INVALID";
    public const string PaymentMethodNotFound = "PAYMENT_METHOD_NOT_FOUND";
    public const string PaymentMethodAlreadyExists = "PAYMENT_METHOD_ALREADY_EXISTS";
    public const string SettingsRoleNotFound = "SETTINGS_ROLE_NOT_FOUND";
    public const string SettingsRoleAlreadyExists = "SETTINGS_ROLE_ALREADY_EXISTS";
    public const string SettingsRoleProtected = "SETTINGS_ROLE_PROTECTED";
    public const string SettingsRoleInUse = "SETTINGS_ROLE_IN_USE";
    public const string OrganizationUserNotFound = "ORGANIZATION_USER_NOT_FOUND";
    public const string OrganizationOwnerRequired = "ORGANIZATION_OWNER_REQUIRED";
    public const string OrganizationInvitationExists = "ORGANIZATION_INVITATION_EXISTS";
    public const string PermissionNotEnabled = "PERMISSION_NOT_ENABLED";
    public const string Validation = "VALIDATION_ERROR";
    public const string Internal = "INTERNAL_ERROR";
}
