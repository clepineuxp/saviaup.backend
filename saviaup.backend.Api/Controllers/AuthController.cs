using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SaviaUp.Backend.Api.Extensions;
using SaviaUp.Backend.Domain.DTOs;
using SaviaUp.Backend.Domain.Ports;
using SaviaUp.Backend.Shared.Localization;

namespace SaviaUp.Backend.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    ILoginUseCase loginUseCase,
    IRegisterUseCase registerUseCase,
    IRefreshTokenUseCase refreshTokenUseCase,
    IForgotPasswordUseCase forgotPasswordUseCase,
    IResetPasswordUseCase resetPasswordUseCase,
    ILogoutUseCase logoutUseCase,
    ICurrentUserContext currentUser) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<RegisterResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
        => this.FromResult(await registerUseCase.ExecuteAsync(request, cancellationToken));

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthSessionDto>> Login(LoginRequest request, CancellationToken cancellationToken)
        => this.FromResult(await loginUseCase.ExecuteAsync(request, cancellationToken));

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<TokenResponse>> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
        => this.FromResult(await refreshTokenUseCase.ExecuteAsync(request, cancellationToken));

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken)
        => this.FromResult(await logoutUseCase.ExecuteAsync(request, currentUser.UserId!.Value, currentUser.SessionId!.Value, cancellationToken));

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<MessageResponse>> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await forgotPasswordUseCase.ExecuteAsync(request, cancellationToken);
        if (!result.IsSuccess) return this.Error(result.Error!);
        var message = TranslationCatalog.Translate(LocalizationKeys.PasswordResetSent, Request.Headers.AcceptLanguage.ToString());
        return Ok(new MessageResponse(message));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
        => this.FromResult(await resetPasswordUseCase.ExecuteAsync(request, cancellationToken));
}
