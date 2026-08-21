using SaviaUp.Backend.Domain.Results;
using SaviaUp.Backend.Shared.Constants;
using SaviaUp.Backend.Shared.Localization;

namespace SaviaUp.Backend.Core.Common;

internal static class Errors
{
    public static readonly Error InvalidCredentials = new(ErrorCodes.InvalidCredentials, LocalizationKeys.InvalidCredentials, ErrorType.Unauthenticated);
    public static readonly Error AccountDisabled = new(ErrorCodes.AccountDisabled, LocalizationKeys.AccountDisabled, ErrorType.Unauthenticated);
    public static readonly Error EmailAlreadyExists = new(ErrorCodes.EmailAlreadyExists, LocalizationKeys.EmailAlreadyExists, ErrorType.Conflict);
    public static readonly Error RefreshInvalid = new(ErrorCodes.RefreshInvalid, LocalizationKeys.RefreshInvalid, ErrorType.Unauthenticated);
    public static readonly Error RefreshExpired = new(ErrorCodes.RefreshExpired, LocalizationKeys.RefreshInvalid, ErrorType.Unauthenticated);
    public static readonly Error PasswordResetInvalid = new(ErrorCodes.PasswordResetInvalid, LocalizationKeys.PasswordResetInvalid, ErrorType.Validation);
    public static readonly Error Validation = new(ErrorCodes.Validation, LocalizationKeys.Validation, ErrorType.Validation);
    public static readonly Error TenantNotFound = new(ErrorCodes.TenantNotFound, LocalizationKeys.TenantNotFound, ErrorType.NotFound);
    public static readonly Error TenantAccessDenied = new(ErrorCodes.TenantAccessDenied, LocalizationKeys.TenantAccessDenied, ErrorType.Forbidden);
    public static readonly Error CategoryNotFound = new(ErrorCodes.CategoryNotFound, LocalizationKeys.CategoryNotFound, ErrorType.NotFound);
    public static readonly Error CategoryNameAlreadyExists = new(ErrorCodes.CategoryNameAlreadyExists, LocalizationKeys.CategoryNameAlreadyExists, ErrorType.Conflict);
    public static readonly Error CategoryInUse = new(ErrorCodes.CategoryInUse, LocalizationKeys.CategoryInUse, ErrorType.Conflict);
    public static readonly Error IngredientNotFound = new(ErrorCodes.IngredientNotFound, LocalizationKeys.IngredientNotFound, ErrorType.NotFound);
    public static readonly Error IngredientInUse = new(ErrorCodes.IngredientInUse, LocalizationKeys.IngredientInUse, ErrorType.Conflict);
    public static readonly Error MeasurementUnitNotFound = new(ErrorCodes.MeasurementUnitNotFound, LocalizationKeys.MeasurementUnitNotFound, ErrorType.NotFound);
    public static readonly Error MeasurementUnitAlreadyExists = new(ErrorCodes.MeasurementUnitAlreadyExists, LocalizationKeys.MeasurementUnitAlreadyExists, ErrorType.Conflict);
    public static readonly Error MeasurementUnitInUse = new(ErrorCodes.MeasurementUnitInUse, LocalizationKeys.MeasurementUnitInUse, ErrorType.Conflict);
    public static readonly Error InventoryMovementInvalid = new(ErrorCodes.InventoryMovementInvalid, LocalizationKeys.InventoryMovementInvalid, ErrorType.Validation);
    public static readonly Error InventoryInsufficientStock = new(ErrorCodes.InventoryInsufficientStock, LocalizationKeys.InventoryInsufficientStock, ErrorType.Business);
    public static readonly Error ProductNotFound = new(ErrorCodes.ProductNotFound, LocalizationKeys.ProductNotFound, ErrorType.NotFound);
    public static readonly Error DiningAreaNotFound = new(ErrorCodes.DiningAreaNotFound, LocalizationKeys.DiningAreaNotFound, ErrorType.NotFound);
    public static readonly Error DiningAreaAlreadyExists = new(ErrorCodes.DiningAreaAlreadyExists, LocalizationKeys.DiningAreaAlreadyExists, ErrorType.Conflict);
    public static readonly Error DiningAreaInUse = new(ErrorCodes.DiningAreaInUse, LocalizationKeys.DiningAreaInUse, ErrorType.Conflict);
    public static readonly Error RestaurantTableNotFound = new(ErrorCodes.RestaurantTableNotFound, LocalizationKeys.RestaurantTableNotFound, ErrorType.NotFound);
    public static readonly Error RestaurantTableAlreadyExists = new(ErrorCodes.RestaurantTableAlreadyExists, LocalizationKeys.RestaurantTableAlreadyExists, ErrorType.Conflict);
    public static readonly Error RestaurantTableOccupied = new(ErrorCodes.RestaurantTableOccupied, LocalizationKeys.RestaurantTableOccupied, ErrorType.Conflict);
    public static readonly Error CashRegisterClosed = new(ErrorCodes.CashRegisterClosed, LocalizationKeys.CashRegisterClosed, ErrorType.Business);
    public static readonly Error OrganizationDocumentOwnerOnly = new(ErrorCodes.OrganizationDocumentOwnerOnly, LocalizationKeys.OrganizationDocumentOwnerOnly, ErrorType.Forbidden);
    public static readonly Error OrganizationLogoInvalid = new(ErrorCodes.OrganizationLogoInvalid, LocalizationKeys.OrganizationLogoInvalid, ErrorType.Validation);
    public static readonly Error PaymentMethodNotFound = new(ErrorCodes.PaymentMethodNotFound, LocalizationKeys.PaymentMethodNotFound, ErrorType.NotFound);
    public static readonly Error PaymentMethodAlreadyExists = new(ErrorCodes.PaymentMethodAlreadyExists, LocalizationKeys.PaymentMethodAlreadyExists, ErrorType.Conflict);
    public static readonly Error SettingsRoleNotFound = new(ErrorCodes.SettingsRoleNotFound, LocalizationKeys.SettingsRoleNotFound, ErrorType.NotFound);
    public static readonly Error SettingsRoleAlreadyExists = new(ErrorCodes.SettingsRoleAlreadyExists, LocalizationKeys.SettingsRoleAlreadyExists, ErrorType.Conflict);
    public static readonly Error SettingsRoleProtected = new(ErrorCodes.SettingsRoleProtected, LocalizationKeys.SettingsRoleProtected, ErrorType.Conflict);
    public static readonly Error SettingsRoleInUse = new(ErrorCodes.SettingsRoleInUse, LocalizationKeys.SettingsRoleInUse, ErrorType.Conflict);
    public static readonly Error OrganizationUserNotFound = new(ErrorCodes.OrganizationUserNotFound, LocalizationKeys.OrganizationUserNotFound, ErrorType.NotFound);
    public static readonly Error OrganizationOwnerRequired = new(ErrorCodes.OrganizationOwnerRequired, LocalizationKeys.OrganizationOwnerRequired, ErrorType.Conflict);
    public static readonly Error OrganizationInvitationExists = new(ErrorCodes.OrganizationInvitationExists, LocalizationKeys.OrganizationInvitationExists, ErrorType.Conflict);
    public static readonly Error PermissionNotEnabled = new(ErrorCodes.PermissionNotEnabled, LocalizationKeys.PermissionNotEnabled, ErrorType.Validation);
    public static readonly Error CashRegisterNotFound = new(ErrorCodes.CashRegisterNotFound, LocalizationKeys.CashRegisterNotFound, ErrorType.NotFound);
    public static readonly Error CashRegisterNameAlreadyExists = new(ErrorCodes.CashRegisterNameAlreadyExists, LocalizationKeys.CashRegisterNameAlreadyExists, ErrorType.Conflict);
    public static readonly Error CashRegisterSingleActiveExceeded = new(ErrorCodes.CashRegisterSingleActiveExceeded, LocalizationKeys.CashRegisterSingleActiveExceeded, ErrorType.Conflict);
    public static readonly Error CashRegisterInUse = new(ErrorCodes.CashRegisterInUse, LocalizationKeys.CashRegisterInUse, ErrorType.Conflict);
}
