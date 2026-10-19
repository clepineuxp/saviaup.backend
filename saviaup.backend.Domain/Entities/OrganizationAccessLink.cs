namespace SaviaUp.Backend.Domain.Entities;

public enum OrganizationAccessLinkKind
{
    JoinRequest,
    Invitation
}

public sealed class OrganizationAccessLink
{
    public Guid Id { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public string? ProtectedCode { get; set; }
    public OrganizationAccessLinkKind Kind { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? RoleId { get; set; }
    public Guid? RequestedByUserId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public Tenant? Tenant { get; set; }
    public User? RequestedByUser { get; set; }
    public User CreatedByUser { get; set; } = null!;
}
