using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaviaUp.Backend.Api.Attributes;
using SaviaUp.Backend.Api.Extensions;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Ports;

namespace SaviaUp.Backend.Api.Controllers;

[ApiController]
[Route("api/images")]
[Authorize]
[RequireTenant]
public sealed class ImagesController(ICurrentUserContext currentUser) : ControllerBase
{
    private Guid TenantId => currentUser.TenantId ?? Guid.Empty;

    [HttpPost]
    public async Task<ActionResult<StoredImageDto>> UploadImage(
        [FromBody] UploadImageRequest request,
        [FromServices] IUploadImageUseCase uploadImageUseCase,
        CancellationToken cancellationToken)
    {
        var result = await uploadImageUseCase.ExecuteAsync(TenantId, request, cancellationToken);
        return this.FromResult(result);
    }

    [HttpPost("file")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<StoredImageDto>> UploadImageFile(
        [FromForm] IFormFile file,
        [FromForm] string module,
        [FromForm] string? entityId,
        [FromServices] IUploadImageUseCase uploadImageUseCase,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { code = "Images.EmptyFile", message = "No se ha proporcionado ningún archivo de imagen." });
        }

        if (file.Length > 2 * 1024 * 1024)
        {
            return BadRequest(new { code = "Images.ExceedsSizeLimit", message = $"La imagen excede el peso máximo permitido de 2 MB (Peso: {file.Length / 1024 / 1024.0:F2} MB)." });
        }

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        var bytes = memoryStream.ToArray();
        var base64String = Convert.ToBase64String(bytes);
        var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "image/png" : file.ContentType;
        var dataUrl = $"data:{contentType};base64,{base64String}";

        var request = new UploadImageRequest(
            Module: module,
            EntityId: entityId,
            FileName: file.FileName,
            ContentType: contentType,
            Base64Content: dataUrl
        );

        var result = await uploadImageUseCase.ExecuteAsync(TenantId, request, cancellationToken);
        return this.FromResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StoredImageDto>> GetImageById(
        Guid id,
        [FromServices] IGetImageByIdUseCase getImageByIdUseCase,
        CancellationToken cancellationToken)
    {
        var result = await getImageByIdUseCase.ExecuteAsync(TenantId, id, cancellationToken);
        return this.FromResult(result);
    }

    [HttpGet("{id:guid}/raw")]
    [AllowAnonymous] // Allows direct <img src="/api/images/{id}/raw"> rendering if needed
    public async Task<IActionResult> GetImageRaw(
        Guid id,
        [FromServices] IGetImageByIdUseCase getImageByIdUseCase,
        CancellationToken cancellationToken)
    {
        var result = await getImageByIdUseCase.ExecuteAsync(TenantId, id, cancellationToken);
        if (!result.IsSuccess || result.Value == null)
        {
            return NotFound();
        }

        var img = result.Value;
        string rawBase64 = img.Base64Content;
        if (rawBase64.Contains(";base64,"))
        {
            var parts = rawBase64.Split(";base64,");
            if (parts.Length == 2) rawBase64 = parts[1];
        }

        try
        {
            byte[] bytes = Convert.FromBase64String(rawBase64);
            return File(bytes, img.ContentType);
        }
        catch
        {
            return BadRequest();
        }
    }

    [HttpGet("module/{module}/entity/{entityId}")]
    public async Task<ActionResult<IReadOnlyCollection<StoredImageSummaryDto>>> GetImagesByEntity(
        string module,
        string entityId,
        [FromServices] IGetImagesByEntityUseCase getImagesByEntityUseCase,
        CancellationToken cancellationToken)
    {
        var result = await getImagesByEntityUseCase.ExecuteAsync(TenantId, module, entityId, cancellationToken);
        return this.FromResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteImage(
        Guid id,
        [FromServices] IDeleteImageUseCase deleteImageUseCase,
        CancellationToken cancellationToken)
    {
        var result = await deleteImageUseCase.ExecuteAsync(TenantId, id, cancellationToken);
        return this.FromResult(result);
    }
}
