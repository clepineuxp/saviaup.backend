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
    public const string Validation = "VALIDATION_ERROR";
    public const string Internal = "INTERNAL_ERROR";
}
