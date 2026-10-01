using SaviaUp.Backend.Core.Printing;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Settings;

public sealed class PrintingTemplateSettingsUseCase(
    ISettingsRepository repository,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork) : IPrintingTemplateSettingsUseCase
{
    public async Task<Result<PrintingTemplateSettingsDto>> GetAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var value = await repository.GetParameterValueAsync(
            tenantId,
            SettingsDefaults.PrintingTemplates,
            cancellationToken);
        return Result<PrintingTemplateSettingsDto>.Success(PrintingTemplateRules.Parse(value));
    }

    public async Task<Result<PrintingTemplateSettingsDto>> UpdateAsync(
        Guid tenantId,
        PrintingTemplateSettingsDto request,
        CancellationToken cancellationToken)
    {
        var normalized = PrintingTemplateRules.Normalize(request);
        var parameters = await repository.GetParametersAsync(tenantId, cancellationToken);
        var parameter = parameters.FirstOrDefault(item => item.Key == SettingsDefaults.PrintingTemplates);
        var now = clock.UtcNow;
        if (parameter is null)
        {
            parameter = new OrganizationParameter
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Key = SettingsDefaults.PrintingTemplates,
                ValueType = "json",
                CreatedAt = now
            };
            await repository.AddParametersAsync([parameter], cancellationToken);
        }

        parameter.Value = PrintingTemplateRules.Serialize(normalized);
        parameter.UpdatedAt = now;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<PrintingTemplateSettingsDto>.Success(normalized);
    }
}
