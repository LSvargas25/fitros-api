using FitRos.Application.Features.Auth.Login;
using FitRos.Application.Features.Auth.Refresh;
using FitRos.Application.Features.Auth.Logout;
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