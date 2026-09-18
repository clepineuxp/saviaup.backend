using System.ComponentModel.DataAnnotations;

namespace SaviaUp.Backend.Domain.DTOs;

public sealed record DigitalMenuStyleDto(
    string TemplateId = "bistro",
    string PrimaryColor = "#10b981",
    string AccentColor = "#f59e0b",
    string BackgroundColor = "#ffffff",
    string TextColor = "#0f172a",
    string FontFamily = "Inter",
    string? WelcomeMessage = "¡Bienvenidos! Descubre nuestra selección de platos.",
    bool ShowImages = true,
    string? BannerUrl = null,
    string HeaderAlignment = "left",
    string SelectedButtonTextColor = "#ffffff",
    string InfoPlacement = "header",
    string LogoPlacement = "header");

public sealed record DigitalMenuItemSummaryDto(
    Guid? Id,
    string ItemType,
    Guid TargetId,
    Guid? CategoryId,
    string Name,
    string? Description,
    decimal? Price,
    Guid? ImageRef,
    int SortOrder,
    bool IsActive);

public sealed record DigitalMenuConfigDto(
    bool Enabled,
    string? Slug,
    bool CanEditSlug,
    DigitalMenuStyleDto Style,
    IReadOnlyCollection<DigitalMenuItemSummaryDto> Categories,
    IReadOnlyCollection<DigitalMenuItemSummaryDto> Products);

public sealed record SaveDigitalMenuItemInput(
    [Required] string ItemType,
    Guid TargetId,
    Guid? CategoryId,
    int SortOrder,
    bool IsActive);

public sealed record SaveDigitalMenuItemsRequest(
    [Required] IReadOnlyCollection<SaveDigitalMenuItemInput> Items);

public sealed record UpdateDigitalMenuParametersRequest(
    bool Enabled,
    [MaxLength(60), RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "El slug solo puede contener letras minúsculas, números y guiones.")] string? Slug);

public sealed record PublicProductDto(
    Guid Id,
    string Name,
    string? Description,
    decimal SalePrice,
    Guid? ImageRef,
    string? Image,
    int SortOrder);

public sealed record PublicCategoryDto(
    Guid Id,
    string Name,
    string? Description,
    Guid? ImageRef,
    string? Image,
    int SortOrder,
    IReadOnlyCollection<PublicProductDto> Products);

public sealed record PublicDigitalMenuDto(
    Guid TenantId,
    string OrganizationName,
    bool HasLogo,
    string? Logo,
    long LogoVersion,
    string? Phone,
    string? Address,
    string? Website,
    DigitalMenuStyleDto Style,
    IReadOnlyCollection<PublicCategoryDto> Categories);
