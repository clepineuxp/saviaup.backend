using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaviaUp.Backend.Domain.Options;
using SaviaUp.Backend.Domain.Ports;

namespace SaviaUp.Backend.Infrastructure.Clients;

public sealed class AdminPlanClient(
    HttpClient httpClient,
    ILogger<AdminPlanClient> logger) : IAdminPlanClient
{
    public async Task<DefaultPlanResponse?> GetDefaultPlanAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.GetAsync("/api/admin/plans/default", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Admin API returned status code {StatusCode} when querying default plan.", response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<DefaultPlanResponse>(cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to reach Admin API to query default plan. A fallback will be used.");
            return null;
        }
    }
}
