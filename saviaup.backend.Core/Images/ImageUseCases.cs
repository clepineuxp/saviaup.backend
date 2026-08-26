using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Core.Images;

public sealed class UploadImageUseCase(
    IStoredImageRepository imageRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : IUploadImageUseCase
{
    private const long MaxSizeBytes = 2 * 1024 * 1024; // 2 MB

    public async Task<Result<StoredImageDto>> ExecuteAsync(Guid tenantId, UploadImageRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Module))
        {
            return Result<StoredImageDto>.Failure(new Error("Images.InvalidModule", "El nombre del módulo es requerido.", ErrorType.Validation));
        }

        if (string.IsNullOrWhiteSpace(request.Base64Content))
        {
            return Result<StoredImageDto>.Failure(new Error("Images.EmptyContent", "El contenido de la imagen en base64 no puede estar vacío.", ErrorType.Validation));
        }

        // Clean base64 string if data URL prefix exists (e.g. data:image/png;base64,...)
        string rawBase64 = request.Base64Content;
        string contentType = request.ContentType;

        if (rawBase64.Contains(";base64,"))
        {
            var parts = rawBase64.Split(";base64,");
            if (parts.Length == 2)
            {
                if (string.IsNullOrWhiteSpace(contentType))
                {
                    contentType = parts[0].Replace("data:", "");
                }
                rawBase64 = parts[1];
            }
        }

        byte[] imageBytes;
        try
        {
            imageBytes = Convert.FromBase64String(rawBase64);
        }
        catch (FormatException)
        {
            return Result<StoredImageDto>.Failure(new Error("Images.InvalidBase64", "El formato Base64 de la imagen no es válido.", ErrorType.Validation));
        }

        if (imageBytes.Length > MaxSizeBytes)
        {
            return Result<StoredImageDto>.Failure(new Error("Images.ExceedsSizeLimit", $"La imagen supera el tamaño máximo permitido de 2 MB ({imageBytes.Length / 1024 / 1024.0:F2} MB cargados).", ErrorType.Validation));
        }

        var now = dateTimeProvider.UtcNow;
        var storedImage = new StoredImage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Module = request.Module.ToLowerInvariant().Trim(),
            EntityId = string.IsNullOrWhiteSpace(request.EntityId) ? null : request.EntityId.Trim(),
            FileName = string.IsNullOrWhiteSpace(request.FileName) ? $"img_{storedImageIdShort(now)}.png" : request.FileName.Trim(),
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "image/png" : contentType.Trim(),
            Base64Content = request.Base64Content,
            FileSize = imageBytes.Length,
            CreatedAt = now,
            UpdatedAt = now
        };

        await imageRepository.AddAsync(storedImage, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new StoredImageDto(
            storedImage.Id,
            storedImage.Module,
            storedImage.EntityId,
            storedImage.FileName,
            storedImage.ContentType,
            storedImage.Base64Content,
            storedImage.FileSize,
            storedImage.CreatedAt
        );

        return Result<StoredImageDto>.Success(dto);
    }

    private static string storedImageIdShort(DateTimeOffset now) => now.Ticks.ToString();
}

public sealed class GetImageByIdUseCase(IStoredImageRepository imageRepository) : IGetImageByIdUseCase
{
    public async Task<Result<StoredImageDto>> ExecuteAsync(Guid tenantId, Guid imageId, CancellationToken cancellationToken)
    {
        var storedImage = await imageRepository.GetByIdAsync(tenantId, imageId, cancellationToken);
        if (storedImage == null)
        {
            return Result<StoredImageDto>.Failure(new Error("Images.NotFound", "La imagen solicitada no existe o no se encuentra disponible.", ErrorType.NotFound));
        }

        var dto = new StoredImageDto(
            storedImage.Id,
            storedImage.Module,
            storedImage.EntityId,
            storedImage.FileName,
            storedImage.ContentType,
            storedImage.Base64Content,
            storedImage.FileSize,
            storedImage.CreatedAt
        );

        return Result<StoredImageDto>.Success(dto);
    }
}

public sealed class GetImagesByEntityUseCase(IStoredImageRepository imageRepository) : IGetImagesByEntityUseCase
{
    public async Task<Result<IReadOnlyCollection<StoredImageSummaryDto>>> ExecuteAsync(Guid tenantId, string module, string entityId, CancellationToken cancellationToken)
    {
        var items = await imageRepository.GetByEntityAsync(tenantId, module.ToLowerInvariant().Trim(), entityId.Trim(), cancellationToken);
        var dtos = items.Select(x => new StoredImageSummaryDto(
            x.Id,
            x.Module,
            x.EntityId,
            x.FileName,
            x.ContentType,
            x.FileSize,
            x.CreatedAt,
            $"/api/images/{x.Id}"
        )).ToList();

        return Result<IReadOnlyCollection<StoredImageSummaryDto>>.Success(dtos);
    }
}

public sealed class DeleteImageUseCase(IStoredImageRepository imageRepository, IUnitOfWork unitOfWork) : IDeleteImageUseCase
{
    public async Task<Result> ExecuteAsync(Guid tenantId, Guid imageId, CancellationToken cancellationToken)
    {
        var storedImage = await imageRepository.GetByIdAsync(tenantId, imageId, cancellationToken);
        if (storedImage == null)
        {
            return Result.Failure(new Error("Images.NotFound", "La imagen a eliminar no existe.", ErrorType.NotFound));
        }

        imageRepository.Remove(storedImage);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
