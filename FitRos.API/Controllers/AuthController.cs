using FitRos.API.Contracts.Auth;
using FitRos.Application.Features.Auth.ForgotPassword;
using FitRos.Application.Features.Auth.Login;
using FitRos.Application.Features.Auth.Logout;
using FitRos.Application.Features.Auth.Refresh;
using FitRos.Application.Features.Auth.ResetPassword;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using ForgotPasswordRequest = FitRos.API.Contracts.Auth.ForgotPasswordRequest;

namespace FitRos.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private const string TagAuth = "Auth";

    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    // =============================
    // LOGIN
    // =============================

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "Login",
        Description = "Authenticates user and returns access and refresh tokens.",
        Tags = new[] { TagAuth })]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginCommand command,
        CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        return Ok(result);
    }

    // =============================
    // FORGOT PASSWORD
    // =============================

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "Forgot password",
        Description = "Initiates password reset process. Always returns 200 OK to prevent email enumeration.",
        Tags = new[] { TagAuth })]
    public async Task<IActionResult> ForgotPassword(
    [FromBody] ForgotPasswordRequest request,
    CancellationToken ct)
    {
        await _sender.Send(
            new ForgotPasswordCommand(request.Email),
            ct);

        return Ok();
    }

    // =============================
    // RESET PASSWORD
    // =============================

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "Reset password",
        Description = "Resets user password using a valid reset token.",
        Tags = new[] { TagAuth })]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordHttpRequest request,
        CancellationToken ct)
    {
        await _sender.Send(
            new ResetPasswordCommand(
                request.Email,
                request.Token,
                request.NewPassword),
            ct);

        return Ok();
    }

    // =============================
    // REFRESH
    // =============================

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RefreshResponse), StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "Refresh token",
        Description = "Rotates refresh token and returns new tokens.",
        Tags = new[] { TagAuth })]
    public async Task<ActionResult<RefreshResponse>> Refresh(
        [FromBody] RefreshCommand command,
        CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        return Ok(result);
    }

    // =============================
    // LOGOUT
    // =============================

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerOperation(
        Summary = "Logout",
        Description = "Revokes refresh token.",
        Tags = new[] { TagAuth })]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutCommand command,
        CancellationToken ct)
    {
        await _sender.Send(command, ct);
        return NoContent();
    }
}