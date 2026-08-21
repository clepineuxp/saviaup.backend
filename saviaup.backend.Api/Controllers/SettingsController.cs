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
[Route("api/settings")]
public sealed class SettingsController(
    IOrganizationSettingsUseCase organization,
    IBusinessSettingsUseCase business,
    IPaymentMethodsSettingsUseCase payments,
    IAccessSettingsUseCase access,
    ICurrentUserContext currentUser) : ControllerBase
{
    private Guid TenantId => currentUser.TenantId!.Value;

    [HttpGet("organization")]
    [RequirePermission(PermissionCodes.SettingsOrganizationRead)]
    public async Task<ActionResult<OrganizationSettingsDto>> GetOrganization(CancellationToken cancellationToken)
        => this.FromResult(await organization.GetAsync(TenantId, currentUser.RoleId!.Value, cancellationToken));

    [HttpPut("organization")]
    [RequirePermission(PermissionCodes.SettingsOrganizationManage)]
    public async Task<ActionResult<OrganizationSettingsDto>> UpdateOrganization(UpdateOrganizationSettingsRequest request, CancellationToken cancellationToken)
        => this.FromResult(await organization.UpdateAsync(TenantId, currentUser.RoleId!.Value, request, cancellationToken));

    [HttpGet("organization/logo")]
    [RequirePermission(PermissionCodes.SettingsOrganizationRead)]
    public async Task<ActionResult> GetOrganizationLogo(CancellationToken cancellationToken)
    {
        var result = await organization.GetLogoAsync(TenantId, cancellationToken);
        return result.IsSuccess ? File(result.Value!.Content, result.Value.ContentType, result.Value.FileName) : this.Error(result.Error!);
    }

    [HttpPost("organization/logo")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(2_200_000)]
    [RequirePermission(PermissionCodes.SettingsOrganizationManage)]
    public async Task<ActionResult> UploadOrganizationLogo([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        return this.FromResult(await organization.UploadLogoAsync(TenantId,
            new UploadOrganizationLogoRequest(stream.ToArray(), file.ContentType, file.FileName), cancellationToken));
    }

    [HttpDelete("organization/logo")]
    [RequirePermission(PermissionCodes.SettingsOrganizationManage)]
    public async Task<ActionResult> DeleteOrganizationLogo(CancellationToken cancellationToken)
        => this.FromResult(await organization.DeleteLogoAsync(TenantId, cancellationToken));

    [HttpGet("business")]
    [RequirePermission(PermissionCodes.SettingsBusinessRead)]
    public async Task<ActionResult<BusinessSettingsDto>> GetBusiness(CancellationToken cancellationToken)
        => this.FromResult(await business.GetAsync(TenantId, cancellationToken));

    [HttpPut("business")]
    [RequirePermission(PermissionCodes.SettingsBusinessManage)]
    public async Task<ActionResult<BusinessSettingsDto>> UpdateBusiness(UpdateBusinessSettingsRequest request, CancellationToken cancellationToken)
        => this.FromResult(await business.UpdateAsync(TenantId, request, cancellationToken));

    [HttpGet("payment-methods")]
    [RequirePermission(PermissionCodes.SettingsPaymentMethodsRead)]
    public async Task<ActionResult<IReadOnlyCollection<PaymentMethodDto>>> ListPaymentMethods([FromQuery] bool includeInactive = false, CancellationToken cancellationToken = default)
        => this.FromResult(await payments.ListAsync(TenantId, includeInactive, cancellationToken));

    [HttpPost("payment-methods")]
    [RequirePermission(PermissionCodes.SettingsPaymentMethodsManage)]
    public async Task<ActionResult<PaymentMethodDto>> CreatePaymentMethod(SavePaymentMethodRequest request, CancellationToken cancellationToken)
        => this.FromResult(await payments.CreateAsync(TenantId, request, cancellationToken));

    [HttpPut("payment-methods/{paymentMethodId:guid}")]
    [RequirePermission(PermissionCodes.SettingsPaymentMethodsManage)]
    public async Task<ActionResult<PaymentMethodDto>> UpdatePaymentMethod(Guid paymentMethodId, SavePaymentMethodRequest request, CancellationToken cancellationToken)
        => this.FromResult(await payments.UpdateAsync(TenantId, paymentMethodId, request, cancellationToken));

    [HttpPatch("payment-methods/{paymentMethodId:guid}/status")]
    [RequirePermission(PermissionCodes.SettingsPaymentMethodsManage)]
    public async Task<ActionResult<PaymentMethodDto>> SetPaymentMethodStatus(Guid paymentMethodId, SetPaymentMethodStatusRequest request, CancellationToken cancellationToken)
        => this.FromResult(await payments.SetStatusAsync(TenantId, paymentMethodId, request, cancellationToken));

    [HttpDelete("payment-methods/{paymentMethodId:guid}")]
    [RequirePermission(PermissionCodes.SettingsPaymentMethodsManage)]
    public async Task<ActionResult> DeletePaymentMethod(Guid paymentMethodId, CancellationToken cancellationToken)
        => this.FromResult(await payments.DeleteAsync(TenantId, paymentMethodId, cancellationToken));

    [HttpGet("access/permissions")]
    [RequirePermission(PermissionCodes.SettingsRolesRead)]
    public async Task<ActionResult<IReadOnlyCollection<EnabledModulePermissionsDto>>> GetPermissions(CancellationToken cancellationToken)
        => this.FromResult(await access.GetPermissionsAsync(TenantId, cancellationToken));

    [HttpGet("access/roles")]
    [RequirePermission(PermissionCodes.SettingsRolesRead)]
    public async Task<ActionResult<IReadOnlyCollection<SettingsRoleDto>>> ListRoles([FromQuery] bool includeInactive = true, CancellationToken cancellationToken = default)
        => this.FromResult(await access.ListRolesAsync(TenantId, includeInactive, cancellationToken));

    [HttpPost("access/roles")]
    [RequirePermission(PermissionCodes.SettingsRolesManage)]
    public async Task<ActionResult<SettingsRoleDto>> CreateRole(SaveSettingsRoleRequest request, CancellationToken cancellationToken)
        => this.FromResult(await access.CreateRoleAsync(TenantId, request, cancellationToken));

    [HttpPut("access/roles/{roleId:guid}")]
    [RequirePermission(PermissionCodes.SettingsRolesManage)]
    public async Task<ActionResult<SettingsRoleDto>> UpdateRole(Guid roleId, SaveSettingsRoleRequest request, CancellationToken cancellationToken)
        => this.FromResult(await access.UpdateRoleAsync(TenantId, roleId, request, cancellationToken));

    [HttpPatch("access/roles/{roleId:guid}/status")]
    [RequirePermission(PermissionCodes.SettingsRolesManage)]
    public async Task<ActionResult<SettingsRoleDto>> SetRoleStatus(Guid roleId, SetSettingsRoleStatusRequest request, CancellationToken cancellationToken)
        => this.FromResult(await access.SetRoleStatusAsync(TenantId, roleId, request, cancellationToken));

    [HttpDelete("access/roles/{roleId:guid}")]
    [RequirePermission(PermissionCodes.SettingsRolesManage)]
    public async Task<ActionResult> DeleteRole(Guid roleId, CancellationToken cancellationToken)
        => this.FromResult(await access.DeleteRoleAsync(TenantId, roleId, cancellationToken));

    [HttpGet("access/users")]
    [RequirePermission(PermissionCodes.SettingsUsersRead)]
    public async Task<ActionResult<IReadOnlyCollection<OrganizationUserDto>>> ListUsers(CancellationToken cancellationToken)
        => this.FromResult(await access.ListUsersAsync(TenantId, cancellationToken));

    [HttpPost("access/users")]
    [RequirePermission(PermissionCodes.SettingsUsersManage)]
    public async Task<ActionResult<OrganizationUserDto>> InviteUser(InviteOrganizationUserRequest request, CancellationToken cancellationToken)
        => this.FromResult(await access.InviteUserAsync(TenantId, currentUser.UserId!.Value, request, cancellationToken));

    [HttpPatch("access/users/{membershipId:guid}")]
    [RequirePermission(PermissionCodes.SettingsUsersManage)]
    public async Task<ActionResult<OrganizationUserDto>> UpdateUser(Guid membershipId, UpdateOrganizationUserRequest request, CancellationToken cancellationToken)
        => this.FromResult(await access.UpdateUserAsync(TenantId, membershipId, request, cancellationToken));

    [HttpDelete("access/users/{entryId:guid}")]
    [RequirePermission(PermissionCodes.SettingsUsersManage)]
    public async Task<ActionResult> DeleteUser(Guid entryId, CancellationToken cancellationToken)
        => this.FromResult(await access.DeleteUserAsync(TenantId, currentUser.UserId!.Value, entryId, cancellationToken));
}
