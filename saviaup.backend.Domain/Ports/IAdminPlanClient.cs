namespace SaviaUp.Backend.Domain.Ports;

public interface IAdminPlanClient
{
    Task<DefaultPlanResponse?> GetDefaultPlanAsync(CancellationToken cancellationToken);
}

public sealed record DefaultPlanResponse(
    Guid Id,
    string Code,
    string Name,
    string Description,
    decimal MonthlyPrice,
    string Currency,
    IReadOnlyCollection<string> PermissionCodes
);
