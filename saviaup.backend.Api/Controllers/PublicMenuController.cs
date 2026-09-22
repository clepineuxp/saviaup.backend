using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaviaUp.Backend.Api.Extensions;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Domain.Results;

namespace SaviaUp.Backend.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/public/menu")]
public sealed class PublicMenuController(IDigitalMenuUseCase digitalMenu) : ControllerBase
{
    [HttpGet("{slug}")]
    public async Task<ActionResult<PublicDigitalMenuDto>> GetPublicMenu(string slug, CancellationToken cancellationToken)
        => this.FromResult(await digitalMenu.GetPublicMenuAsync(slug, cancellationToken));

    [HttpGet("{slug}/images/{imageId:guid}")]
    public async Task<IActionResult> GetPublicMenuImage(
        string slug,
        Guid imageId,
        CancellationToken cancellationToken)
        => await GetImageResultAsync(
            digitalMenu.GetPublicMenuImageAsync(slug, imageId, cancellationToken));

    [HttpGet("{slug}/logo")]
    public async Task<IActionResult> GetPublicMenuLogo(string slug, CancellationToken cancellationToken)
        => await GetImageResultAsync(digitalMenu.GetPublicMenuLogoAsync(slug, cancellationToken));

    private async Task<IActionResult> GetImageResultAsync(Task<Result<PublicDigitalMenuImageDto>> imageTask)
    {
        var result = await imageTask;
        if (!result.IsSuccess || result.Value is null)
        {
            return this.Error(result.Error!);
        }

        Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        Response.Headers.ETag = $"\"{result.Value.Version}\"";
        return File(result.Value.Content, result.Value.ContentType);
    }
}
