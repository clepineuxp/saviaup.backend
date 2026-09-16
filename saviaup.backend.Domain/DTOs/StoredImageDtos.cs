namespace SaviaUp.Backend.Domain.DTOs;

public record UploadImageRequest(
    string Module,
    string? EntityId,
    string FileName,
    string ContentType,
    string Base64Content
);

public record StoredImageDto(
    Guid Id,
    string Module,
    string? EntityId,
    string FileName,
    string ContentType,
    string Base64Content,
    long FileSize,
    DateTimeOffset CreatedAt
);

public record StoredImageSummaryDto(
    Guid Id,
    string Module,
    string? EntityId,
    string FileName,
    string ContentType,
    long FileSize,
    DateTimeOffset CreatedAt,
    string Url
);
