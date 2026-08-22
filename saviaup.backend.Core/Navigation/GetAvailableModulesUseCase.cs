using SaviaUp.Backend.Core.Common;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;
using SaviaUp.Backend.Shared.Localization;

namespace SaviaUp.Backend.Core.Navigation;

public sealed class GetAvailableModulesUseCase(
    IModuleRepository moduleRepository,
    ITenantRepository tenantRepository,
    IRoleRepository roleRepository,
    IPermissionRepository permissionRepository,
    IDateTimeProvider clock) : IGetAvailableModulesUseCase
{
    public async Task<Result<AvailableModulesResponse>> ExecuteAsync(
        Guid userId,
        Guid tenantId,
        Guid roleId,
        string? language,
        CancellationToken cancellationToken)
    {
        var membership = await tenantRepository.GetMembershipAsync(userId, tenantId, cancellationToken);
        var role = membership is not null ? await roleRepository.GetByIdAsync(membership.RoleId, cancellationToken) : null;

        if (membership is null
            || !membership.IsEnabledAt(clock.UtcNow)
            || !membership.Tenant.IsActive
            || role is null
            || !role.IsActive
            || membership.RoleId != roleId)
        {
            return Result<AvailableModulesResponse>.Failure(Errors.TenantAccessDenied);
        }

        var availableModules = await moduleRepository.GetAvailableForRoleAsync(tenantId, roleId, cancellationToken);
        var modulesByCode = availableModules.ToDictionary(module => module.Code, StringComparer.OrdinalIgnoreCase);
        var unconfiguredModule = modulesByCode.Keys.FirstOrDefault(code => !NavigationCatalog.Modules.ContainsKey(code));
        if (unconfiguredModule is not null)
        {
            throw new InvalidOperationException(
                $"Module '{unconfiguredModule}' must define its navigation section and order in NavigationCatalog.");
        }

        var permissions = await permissionRepository.GetForRoleAsync(tenantId, roleId, cancellationToken);
        var permissionSet = permissions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var sections = NavigationCatalog.Sections
            .OrderBy(section => section.Order)
            .Select(section => MapSection(section, modulesByCode, permissionSet, language))
            .Where(section => section.Modules.Count > 0 || section.Options.Count > 0)
            .ToArray();
        var emptyStateMessage = sections.Length == 0
            ? TranslationCatalog.Translate(LocalizationKeys.NavigationNoModules, language)
            : null;

        return Result<AvailableModulesResponse>.Success(new AvailableModulesResponse(sections, emptyStateMessage));
    }

    private static AvailableModuleSectionDto MapSection(
        NavigationSectionDefinition section,
        IReadOnlyDictionary<string, AvailableModuleReference> modulesByCode,
        IReadOnlySet<string> permissions,
        string? language)
    {
        var modules = section.Modules
            .Where(module => modulesByCode.ContainsKey(module.Code))
            .OrderBy(module => module.Order)
            .Select(module => new AvailableModuleDto(
                modulesByCode[module.Code].Id,
                module.Code,
                TranslationCatalog.Translate(LocalizationKeys.ModuleName(module.Code), language),
                module.Order))
            .ToArray();
        var options = section.Options
            .Where(option => permissions.Contains(option.RequiredPermissionCode))
            .OrderBy(option => option.Order)
            .Select(option => new AvailableNavigationOptionDto(
                option.Code,
                option.ModuleCode,
                TranslationCatalog.Translate(LocalizationKeys.NavigationOptionName(option.Code), language),
                option.Order))
            .ToArray();

        return new AvailableModuleSectionDto(
            section.Code,
            TranslationCatalog.Translate(LocalizationKeys.NavigationSectionName(section.Code), language),
            section.Order,
            modules.Length + options.Length > 1,
            modules,
            options);
    }
}
