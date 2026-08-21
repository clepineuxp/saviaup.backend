using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaviaUp.Backend.Api.Attributes;
using SaviaUp.Backend.Api.Extensions;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Shared.Constants;

namespace SaviaUp.Backend.Api.Controllers;

[ApiController]
[Authorize]
[RequireTenant]
[Route("api/products")]
public sealed class ProductsController(
    IListProductsUseCase listUseCase,
    ICreateProductUseCase createUseCase,
    IUpdateProductUseCase updateUseCase,
    ISetProductStatusUseCase statusUseCase,
    IDeleteProductUseCase deleteUseCase,
    ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCodes.ProductsRead)]
    public async Task<ActionResult<PagedResponse<ProductDto>>> List(
        [FromQuery] ProductQueryRequest request,
        CancellationToken cancellationToken)
        => this.FromResult(await listUseCase.ExecuteAsync(
            currentUser.TenantId!.Value,
            request,
            cancellationToken));

    [HttpPost]
    [RequirePermission(PermissionCodes.ProductsManage)]
    public async Task<ActionResult<ProductDto>> Create(
        CreateProductRequest request,
        CancellationToken cancellationToken)
        => this.FromResult(await createUseCase.ExecuteAsync(
            currentUser.TenantId!.Value,
            request,
            cancellationToken));

    [HttpPut("{productId:guid}")]
    [RequirePermission(PermissionCodes.ProductsManage)]
    public async Task<ActionResult<ProductDto>> Update(
        Guid productId,
        UpdateProductRequest request,
        CancellationToken cancellationToken)
        => this.FromResult(await updateUseCase.ExecuteAsync(
            currentUser.TenantId!.Value,
            productId,
            request,
            cancellationToken));

    [HttpPatch("{productId:guid}/status")]
    [RequirePermission(PermissionCodes.ProductsManage)]
    public async Task<ActionResult<ProductDto>> SetStatus(
        Guid productId,
        SetProductStatusRequest request,
        CancellationToken cancellationToken)
        => this.FromResult(await statusUseCase.ExecuteAsync(
            currentUser.TenantId!.Value,
            productId,
            request,
            cancellationToken));

    [HttpDelete("{productId:guid}")]
    [RequirePermission(PermissionCodes.ProductsManage)]
    public async Task<ActionResult> Delete(Guid productId, CancellationToken cancellationToken)
        => this.FromResult(await deleteUseCase.ExecuteAsync(
            currentUser.TenantId!.Value,
            productId,
            cancellationToken));
}
