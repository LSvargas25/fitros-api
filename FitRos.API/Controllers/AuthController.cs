using FitRos.API.Contracts.Auth;
using FitRos.Application.Features.Auth.ForgotPassword;
using FitRos.Application.Features.Auth.Login;
using FitRos.Application.Features.Auth.Logout;
using FitRos.Application.Features.Auth.Refresh;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FitRos.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private const string TagAuth = "Auth";

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Login", Description = "Authenticates user and returns access/refresh tokens.", Tags = new[] { TagAuth })]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginCommand command,
        [FromServices] LoginHandler handler,
        CancellationToken ct)
    {
        var result = await handler.Handle(command, ct);
        return Ok(result);
    }

    private readonly ISender _sender;

    public AuthController(ISender sender) => _sender = sender;


    [SwaggerOperation(
    Summary = "Forgot password",
    Description = "Initiates password reset process. Always returns 200 OK to prevent email enumeration.",
    Tags = new[] { TagAuth }
)]
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken ct)
    {
        await _sender.Send(new ForgotPasswordCommand(request.Email), ct);
        return Ok();
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshResponse), StatusCodes.Status200OK)]
    [SwaggerOperation(Summary = "Refresh token", Description = "Rotates refresh token and returns new tokens.", Tags = new[] { TagAuth })]
    public async Task<ActionResult<RefreshResponse>> Refresh(
        [FromBody] RefreshCommand command,
        [FromServices] RefreshHandler handler,
        CancellationToken ct)
    {
        var result = await handler.Handle(command, ct);
        return Ok(result);
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerOperation(Summary = "Logout", Description = "Revokes refresh token.", Tags = new[] { TagAuth })]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutCommand command,
        [FromServices] LogoutHandler handler,
        CancellationToken ct)
    {
        await handler.Handle(command, ct);
        return NoContent();
    }
}