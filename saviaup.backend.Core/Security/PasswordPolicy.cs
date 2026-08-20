using Microsoft.Extensions.Options;
using SaviaUp.Backend.Domain.Options;

namespace SaviaUp.Backend.Core.Security;

public sealed class PasswordPolicy(IOptions<PasswordPolicyOptions> options)
{
    private readonly PasswordPolicyOptions _options = options.Value;

    public bool IsValid(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < _options.MinimumLength) return false;
        if (_options.RequireUppercase && !password.Any(char.IsUpper)) return false;
        if (_options.RequireLowercase && !password.Any(char.IsLower)) return false;
        if (_options.RequireDigit && !password.Any(char.IsDigit)) return false;
        return !_options.RequireSpecialCharacter || password.Any(character => !char.IsLetterOrDigit(character));
    }
}
