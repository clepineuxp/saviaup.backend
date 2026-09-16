using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaviaUp.Backend.Api.Attributes;
using SaviaUp.Backend.Api.Extensions;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Shared.Constants;

namespace SaviaUp.Backend.Api.Controllers;

[ApiController]
[Route("api/statistics")]
[Authorize]
[RequireTenant]
public sealed class StatisticsController(
    IGetStatisticsUseCase getStatisticsUseCase,
    ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCodes.OrdersRead)]
    public async Task<ActionResult<StatisticsDashboardDto>> GetStatistics(
        [FromQuery] string? period = "current_month",
        [FromQuery] bool? includeTips = false,
        CancellationToken cancellationToken = default)
    {
        var result = await getStatisticsUseCase.ExecuteAsync(currentUser.TenantId!.Value, period, includeTips, cancellationToken);
        return this.FromResult(result);
    }
}
