using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Settings;

public sealed class OrganizationSettingsUseCase(
    ISettingsRepository repository,
    IRoleRepository roleRepository,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork) : IOrganizationSettingsUseCase
{
    private static readonly HashSet<string> LogoTypes = new(StringComparer.OrdinalIgnoreCase) { "image/png", "image/jpeg", "image/webp" };

    public async Task<Result<OrganizationSettingsDto>> GetAsync(Guid tenantId, Guid roleId, CancellationToken cancellationToken)
    {
        var tenant = await repository.GetTenantForUpdateAsync(tenantId, cancellationToken);
        if (tenant is null) return Result<OrganizationSettingsDto>.Failure(Errors.TenantNotFound);
        var role = await roleRepository.GetByIdAsync(roleId, cancellationToken);
        return Result<OrganizationSettingsDto>.Success(Map(tenant, role?.TenantId == tenantId && role.Code == "TENANT_OWNER"));
    }

    public async Task<Result<OrganizationSettingsDto>> UpdateAsync(Guid tenantId, Guid roleId, UpdateOrganizationSettingsRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return Result<OrganizationSettingsDto>.Failure(Errors.Validation);
        var tenant = await repository.GetTenantForUpdateAsync(tenantId, cancellationToken);
        if (tenant is null) return Result<OrganizationSettingsDto>.Failure(Errors.TenantNotFound);
        var role = await roleRepository.GetByIdAsync(roleId, cancellationToken);
        var isOwner = role?.TenantId == tenantId && role.Code == "TENANT_OWNER";
        var document = Clean(request.Document);
        if (!isOwner && !string.Equals(document, tenant.Document, StringComparison.Ordinal))
            return Result<OrganizationSettingsDto>.Failure(Errors.OrganizationDocumentOwnerOnly);

        tenant.Name = SettingsDefaults.Normalize(request.Name);
        tenant.ResponsibleName = Clean(request.ResponsibleName);
        tenant.Document = document;
        tenant.ContactName = Clean(request.ContactName);
        tenant.Email = Clean(request.Email);
        tenant.Address = Clean(request.Address);
        tenant.Country = Clean(request.Country);
        tenant.State = Clean(request.State);
        tenant.City = Clean(request.City);
        tenant.Phone = Clean(request.Phone);
        tenant.Website = Clean(request.Website);
        tenant.UpdatedAt = clock.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<OrganizationSettingsDto>.Success(Map(tenant, isOwner));
    }

    public async Task<Result<OrganizationLogoDto>> GetLogoAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = await repository.GetTenantForUpdateAsync(tenantId, cancellationToken);
        if (tenant?.LogoData is null || tenant.LogoContentType is null) return Result<OrganizationLogoDto>.Failure(Errors.TenantNotFound);
        return Result<OrganizationLogoDto>.Success(new OrganizationLogoDto(tenant.LogoData, tenant.LogoContentType, tenant.LogoFileName ?? "logo"));
    }

    public async Task<Result> UploadLogoAsync(Guid tenantId, UploadOrganizationLogoRequest request, CancellationToken cancellationToken)
    {
        if (request.Content.Length is 0 or > 2_097_152 || !LogoTypes.Contains(request.ContentType)) return Result.Failure(Errors.OrganizationLogoInvalid);
        var tenant = await repository.GetTenantForUpdateAsync(tenantId, cancellationToken);
        if (tenant is null) return Result.Failure(Errors.TenantNotFound);
        tenant.LogoData = request.Content;
        tenant.LogoContentType = request.ContentType;
        tenant.LogoFileName = Path.GetFileName(request.FileName ?? "logo");
        tenant.UpdatedAt = clock.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> DeleteLogoAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = await repository.GetTenantForUpdateAsync(tenantId, cancellationToken);
        if (tenant is null) return Result.Failure(Errors.TenantNotFound);
        tenant.LogoData = null; tenant.LogoContentType = null; tenant.LogoFileName = null; tenant.UpdatedAt = clock.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static OrganizationSettingsDto Map(Tenant tenant, bool canEditDocument) => new(
        tenant.Id, tenant.Name, tenant.ResponsibleName, tenant.Document, tenant.ContactName, tenant.Email, tenant.Address,
        tenant.Country, tenant.State, tenant.City, tenant.Phone, tenant.Website, tenant.LogoData is not null,
        tenant.UpdatedAt.ToUnixTimeMilliseconds(), canEditDocument);
}
