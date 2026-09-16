using System.ComponentModel.DataAnnotations;

namespace SaviaUp.Backend.Domain.DTOs;

public sealed record SupplierDto(
    Guid Id,
    string Name,
    string? CommercialName,
    string? Document,
    string? Email,
    string? Phone,
    string? Address,
    bool IsActive,
    string? CreatedByUserName,
    string? LastModifiedByUserName,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record SupplierLookupDto(
    Guid Id,
    string Name,
    string? CommercialName);

public sealed record CreateSupplierRequest(
    [Required, MaxLength(160)] string Name,
    [MaxLength(160)] string? CommercialName,
    [MaxLength(80)] string? Document,
    [EmailAddress, MaxLength(320)] string? Email,
    [MaxLength(50)] string? Phone,
    [MaxLength(500)] string? Address);

public sealed record UpdateSupplierRequest(
    [Required, MaxLength(160)] string Name,
    [MaxLength(160)] string? CommercialName,
    [MaxLength(80)] string? Document,
    [EmailAddress, MaxLength(320)] string? Email,
    [MaxLength(50)] string? Phone,
    [MaxLength(500)] string? Address);

public sealed record SetSupplierStatusRequest(bool IsActive);

public sealed record SupplierPageDto(
    IReadOnlyCollection<SupplierDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
