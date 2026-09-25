using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using SaviaUp.Backend.Domain.Options;
using SaviaUp.Backend.Domain.Ports;

namespace SaviaUp.Backend.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("pvc")]
public sealed class FileStorageController(
    IFileStorage fileStorage,
    IOptions<FileStorageOptions> configuredOptions) : ControllerBase
{
    [HttpGet("{tenantId:guid}/{**path}")]
    [ResponseCache(Duration = 31_536_000, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Get(
        Guid tenantId,
        string path,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty || string.IsNullOrWhiteSpace(path)) return NotFound();

        var reference = $"{tenantId:D}/{path}";
        var storedFile = await fileStorage.GetAsync(tenantId, reference, cancellationToken);
        if (storedFile is null) return NotFound();

        var cacheSeconds = Math.Max(1, configuredOptions.Value.CacheDurationDays) * 86_400;
        Response.Headers.CacheControl = $"public,max-age={cacheSeconds},immutable";
        Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";
        return File(
            storedFile.Content,
            storedFile.ContentType,
            lastModified: storedFile.LastModified,
            entityTag: new EntityTagHeaderValue($"\"{storedFile.LastModified.ToUnixTimeMilliseconds():x}-{storedFile.Content.LongLength:x}\""),
            enableRangeProcessing: true);
    }
}
