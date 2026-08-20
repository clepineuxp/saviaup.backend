namespace SaviaUp.Backend.Domain.Options;

public sealed class PasswordPolicyOptions
{
    public const string SectionName = "PasswordPolicy";
    public int MinimumLength { get; set; } = 10;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireDigit { get; set; } = true;
    public bool RequireSpecialCharacter { get; set; } = true;
}
