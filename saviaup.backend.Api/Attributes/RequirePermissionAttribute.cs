namespace SaviaUp.Backend.Api.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute(string permissionCode) : Attribute
{
    public string PermissionCode { get; } = permissionCode;
}
