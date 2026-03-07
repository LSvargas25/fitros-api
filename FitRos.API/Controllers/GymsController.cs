using FitRos.Application.Features.Gyms.CreateGym;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FitRos.API.Controllers;

[ApiController]
[Route("api/gyms")]
[Authorize]
public sealed class GymsController : ControllerBase
{
    private const string TagGyms = "Gyms";

    private readonly IMediator _mediator;

    public GymsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [SwaggerOperation(
        Summary = "Create Gym",
        Description = "Creates a new gym and assigns an admin.",
        Tags = new[] { TagGyms })]
    public async Task<IActionResult> CreateGym(
        [FromBody] CreateGymCommand command,
        CancellationToken cancellationToken)
    {
        var gymId = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(CreateGym), new { id = gymId }, gymId);
    }
}