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
}
