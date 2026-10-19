using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaviaUp.Backend.Api.Attributes;
using SaviaUp.Backend.Api.Extensions;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Shared.Constants;

namespace SaviaUp.Backend.Api.Controllers;

[ApiController]
[Route("api/organization-links")]
public sealed class OrganizationAccessLinksController(
    IOrganizationAccessLinkUseCase useCase,
    ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet("{code}")]
    [AllowAnonymous]
    public async Task<ActionResult<OrganizationAccessLinkDto>> Preview(string code, CancellationToken cancellationToken)
        => this.FromResult(await useCase.PreviewAsync(code, currentUser.UserId, cancellationToken));

    [HttpPost("join-requests")]
    [Authorize]
    public async Task<ActionResult<CreateJoinRequestResponse>> CreateJoinRequest(CancellationToken cancellationToken)
        => this.FromResult(await useCase.CreateJoinRequestAsync(currentUser.UserId!.Value, cancellationToken));

    [HttpPost("invitations")]
    [Authorize]
    [RequireTenant]
    [RequirePermission(PermissionCodes.SettingsUsersManage)]
    public async Task<ActionResult<CreatedOrganizationAccessLinkDto>> CreateInvitation(CreateOrganizationInvitationLinkRequest request, CancellationToken cancellationToken)
        => this.FromResult(await useCase.CreateInvitationAsync(currentUser.TenantId!.Value, currentUser.UserId!.Value, request, cancellationToken));

    [HttpGet("invitations")]
    [Authorize]
    [RequireTenant]
    [RequirePermission(PermissionCodes.SettingsUsersRead)]
    public async Task<ActionResult<IReadOnlyCollection<OrganizationAccessLinkSummaryDto>>> ListInvitations(CancellationToken cancellationToken)
        => this.FromResult(await useCase.ListInvitationsAsync(currentUser.TenantId!.Value, cancellationToken));

    [HttpPost("{code}/accept")]
    [Authorize]
    public async Task<ActionResult<AcceptOrganizationAccessLinkResponse>> Accept(string code, AcceptOrganizationAccessLinkRequest request, CancellationToken cancellationToken)
        => this.FromResult(await useCase.AcceptAsync(currentUser.UserId!.Value, code, request, cancellationToken));
}
