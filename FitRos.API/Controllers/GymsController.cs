using FitRos.Application.Features.Gyms.ActivateGym;
using FitRos.Application.Features.Gyms.AssignAdminToGym;
using FitRos.Application.Features.Gyms.CreateGym;
using FitRos.Application.Features.Gyms.DeactivateGym;
using FitRos.Application.Features.Gyms.GetAllGyms;
using FitRos.Application.Features.Gyms.GetGymById;
using FitRos.Application.Features.Gyms.GetGymsCount;
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

    [HttpGet]
    [ProducesResponseType(typeof(List<GymListItemDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "List all gyms",
        Description = "Returns all gyms with optional admin info. OwnerApp only.",
        Tags = new[] { TagGyms })]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAllGymsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{gymId:guid}")]
    [ProducesResponseType(typeof(GymDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Get gym by id",
        Description = "Returns full gym info. OwnerApp sees any gym; Admin only their own.",
        Tags = new[] { TagGyms })]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid gymId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetGymByIdQuery(gymId), cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet("total")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "Get gyms count",
        Description = "Returns total number of active (non-deleted) gyms. OwnerApp only.",
        Tags = new[] { TagGyms })]
    public async Task<IActionResult> GetTotal(CancellationToken cancellationToken)
    {
        var count = await _mediator.Send(new GetGymsCountQuery(), cancellationToken);
        return Ok(count);
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

        return CreatedAtAction(nameof(GetById), new { gymId }, gymId);
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
       Description = "Activates a gym that was previously deactivated.",
       Tags = new[] { TagGyms })]
    public async Task<IActionResult> ActivateGym(
       [FromRoute] Guid gymId,
       CancellationToken cancellationToken)
    {
        await _mediator.Send(new ActivateGymCommand(gymId), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{gymId:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerOperation(
        Summary = "Deactivate Gym",
        Description = "Deactivates a gym.",
        Tags = new[] { TagGyms })]
    public async Task<IActionResult> DeactivateGym(
        [FromRoute] Guid gymId,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeactivateGymCommand(gymId), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{gymId:guid}/assign-admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerOperation(
        Summary = "Assign admin to gym",
        Description = "Assigns an unassigned admin user to the specified gym. OwnerApp only.",
        Tags = new[] { TagGyms })]
    public async Task<IActionResult> AssignAdmin(
        [FromRoute] Guid gymId,
        [FromBody] AssignAdminRequest body,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new AssignAdminToGymCommand(gymId, body.AdminId), cancellationToken);
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
        await _mediator.Send(new SoftDeleteGymCommand(gymId), cancellationToken);
        return NoContent();
    }
}

public sealed record AssignAdminRequest(Guid AdminId);
