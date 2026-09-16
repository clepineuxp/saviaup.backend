using System.ComponentModel.DataAnnotations;

namespace SaviaUp.Backend.Domain.DTOs;

public sealed record CategoryDto(
    Guid Id,
    string Name,
    string? Description,
    string? Image,
    bool IsInventoryTracked,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateCategoryRequest(
    [Required, MaxLength(120)] string Name,
    [MaxLength(1000)] string? Description,
    string? Image,
    bool IsInventoryTracked);

public sealed record UpdateCategoryRequest(
    [Required, MaxLength(120)] string Name,
    [MaxLength(1000)] string? Description,
    string? Image,
    bool IsInventoryTracked);

public sealed record SetCategoryStatusRequest(bool IsActive);
