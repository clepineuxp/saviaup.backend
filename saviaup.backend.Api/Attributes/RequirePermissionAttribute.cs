namespace SaviaUp.Backend.Api.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : Attribute
{
    public string[] PermissionCodes { get; }

    public RequirePermissionAttribute(string permissionCode)
    {
        PermissionCodes = new[] { permissionCode };
    }

    public RequirePermissionAttribute(params string[] permissionCodes)
    {
        PermissionCodes = permissionCodes;
    }
}
