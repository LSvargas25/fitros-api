using FitRos.Application.Features.Gyms.ActivateGym;
using FitRos.Application.Features.Gyms.CreateGym;
using FitRos.Application.Features.Gyms.DeactivateGym;
using FitRos.Application.Features.Gyms.SoftDeleteGym;
using FitRos.Application.Features.Gyms.UpdateGym;
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

    [HttpPut("{gymId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerOperation(
        Summary = "Update Gym",
        Description = "Updates the basic information of an existing gym.",
        Tags = new[] { TagGyms })]
    public async Task<IActionResult> UpdateGym(
        [FromRoute] Guid gymId,
        [FromBody] UpdateGymCommand command,
        CancellationToken cancellationToken)
    {
        var updateCommand = command with { GymId = gymId };

        await _mediator.Send(updateCommand, cancellationToken);

        return NoContent();
    }

    [HttpPatch("{gymId:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerOperation(
       Summary = "Activate Gym",
       Description = "Activates a gym that was previously deactivated. Once activated, the gym becomes available for normal operations.",
       Tags = new[] { TagGyms })]
    public async Task<IActionResult> ActivateGym(
       [FromRoute] Guid gymId,
       CancellationToken cancellationToken)
    {
        var command = new ActivateGymCommand(gymId);

        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }

    [HttpPatch("{gymId:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerOperation(
        Summary = "Deactivate Gym",
        Description = "Deactivates a gym. A deactivated gym cannot accept new members or operations until it is activated again.",
        Tags = new[] { TagGyms })]
    public async Task<IActionResult> DeactivateGym(
        [FromRoute] Guid gymId,
        CancellationToken cancellationToken)
    {
        var command = new DeactivateGymCommand(gymId);

        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }
    [HttpDelete("{gymId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerOperation(
       Summary = "Delete Gym",
       Description = "Soft deletes a gym.",
       Tags = new[] { TagGyms })]
    public async Task<IActionResult> DeleteGym(
       [FromRoute] Guid gymId,
       CancellationToken cancellationToken)
    {
        var command = new SoftDeleteGymCommand(gymId);

        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }




}
