namespace SaviaUp.Backend.Domain.DTOs;

public record CashRegisterDto(
    Guid Id,
    string Name,
    string? Location,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record CreateCashRegisterRequest(
    string Name,
    string? Location,
    bool IsActive = true);

public record UpdateCashRegisterRequest(
    string Name,
    string? Location,
    bool IsActive);

public record SetCashRegisterStatusRequest(
    bool IsActive);
