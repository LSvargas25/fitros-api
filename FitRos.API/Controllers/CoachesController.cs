using FitRos.Application.Features.Users.Coach.CreateCoach;
using FitRos.Application.Features.Users.UserManagement.CreateUser;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FitRos.API.Controllers;

[ApiController]
[Route("api/coaches")]
public sealed class CoachesController : ControllerBase
{
    private const string TagCoaches = "Coaches";

    private readonly ISender _sender;

    public CoachesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateUserResponse), StatusCodes.Status201Created)]
    [SwaggerOperation(
        Summary = "Create coach",
        Description = "Creates a new Coach user. OwnerApp can optionally specify GymId; Admin inherits own gym.",
        Tags = new[] { TagCoaches })]
    public async Task<ActionResult<CreateUserResponse>> CreateCoach(
        [FromBody] CreateCoachCommand command,
        CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        return CreatedAtAction(nameof(UsersController.GetById), "Users", new { id = result.Id }, result);
    }
}
