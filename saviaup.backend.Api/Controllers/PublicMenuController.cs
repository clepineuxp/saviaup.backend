using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaviaUp.Backend.Api.Extensions;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Ports;

namespace SaviaUp.Backend.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/public/menu")]
public sealed class PublicMenuController(IDigitalMenuUseCase digitalMenu) : ControllerBase
{
    [HttpGet("{slug}")]
    public async Task<ActionResult<PublicDigitalMenuDto>> GetPublicMenu(string slug, CancellationToken cancellationToken)
        => this.FromResult(await digitalMenu.GetPublicMenuAsync(slug, cancellationToken));

    [HttpGet("{slug}/categories/{categoryId:guid}/images")]
    public async Task<ActionResult<PublicDigitalMenuCategoryImagesDto>> GetPublicMenuCategoryImages(
        string slug,
        Guid categoryId,
        CancellationToken cancellationToken)
        => this.FromResult(await digitalMenu.GetPublicMenuCategoryImagesAsync(
            slug,
            categoryId,
            cancellationToken));
}
