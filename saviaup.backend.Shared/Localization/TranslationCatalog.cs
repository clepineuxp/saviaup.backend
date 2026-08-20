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
                    [LocalizationKeys.TenantRequired] = "Selecciona una organización para continuar.",
                    [LocalizationKeys.TenantAccessDenied] = "No tienes acceso a esta organización.",
                    [LocalizationKeys.TenantNotFound] = "No encontramos la organización solicitada.",
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
                    [LocalizationKeys.TenantRequired] = "Select an organization to continue.",
                    [LocalizationKeys.TenantAccessDenied] = "You don't have access to this organization.",
                    [LocalizationKeys.TenantNotFound] = "The requested organization was not found.",
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
