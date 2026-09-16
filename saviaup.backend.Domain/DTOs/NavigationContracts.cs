namespace SaviaUp.Backend.Domain.DTOs;

public sealed record AvailableModuleReference(Guid Id, string Code);

public sealed record AvailableModuleDto(Guid Id, string Code, string Name, int Order);

public sealed record AvailableNavigationOptionDto(
    string Code,
    string ModuleCode,
    string Name,
    int Order);

public sealed record AvailableModuleSectionDto(
    string Code,
    string Name,
    int Order,
    bool IsGrouped,
    IReadOnlyCollection<AvailableModuleDto> Modules,
    IReadOnlyCollection<AvailableNavigationOptionDto> Options);

public sealed record AvailableModulesResponse(
    IReadOnlyCollection<AvailableModuleSectionDto> Sections,
    string? EmptyStateMessage);

public sealed record UserInfoDto(
    string FirstName,
    string LastName,
    ActiveTenantDto Organization,
    RoleDto Role);
