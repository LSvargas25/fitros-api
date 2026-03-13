using FitRos.Application.Features.Users.Coach.ActivateCoach;
using FitRos.Application.Features.Users.Coach.CreateCoach;
using FitRos.Application.Features.Users.Coach.DeactivateCoach;
using FitRos.Application.Features.Users.Coach.DeleteCoach;
using FitRos.Application.Features.Users.Coach.GetCoachById;
using FitRos.Application.Features.Users.Coach.GetCoaches;
using FitRos.Application.Features.Users.Coach.GetCoachesCount;
using FitRos.Application.Features.Users.Coach.UpdateCoach;
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

    [HttpGet]
    [ProducesResponseType(typeof(List<CoachListItemDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "List coaches",
        Description = "Returns coaches. OwnerApp sees all; Admin sees own gym's coaches.",
        Tags = new[] { TagCoaches })]
    public async Task<IActionResult> GetCoaches(CancellationToken ct)
    {
        var result = await _sender.Send(new GetCoachesQuery(), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CoachDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Get coach by id",
        Description = "Returns coach details. OwnerApp sees any coach; Admin sees own gym only.",
        Tags = new[] { TagCoaches })]
    public async Task<IActionResult> GetCoachById(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetCoachByIdQuery(id), ct);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CoachDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Update coach",
        Description = "Updates coach name/email. OwnerApp can update any coach; Admin updates own gym only.",
        Tags = new[] { TagCoaches })]
    public async Task<IActionResult> UpdateCoach(Guid id, [FromBody] UpdateCoachCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command with { CoachId = id }, ct);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Activate coach",
        Description = "Reactivates an inactive coach. OwnerApp or Admin (own gym).",
        Tags = new[] { TagCoaches })]
    public async Task<IActionResult> ActivateCoach(Guid id, CancellationToken ct)
    {
        await _sender.Send(new ActivateCoachCommand(id), ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Deactivate coach",
        Description = "Deactivates an active coach. OwnerApp or Admin (own gym).",
        Tags = new[] { TagCoaches })]
    public async Task<IActionResult> DeactivateCoach(Guid id, CancellationToken ct)
    {
        await _sender.Send(new DeactivateCoachCommand(id), ct);
        return NoContent();
    }

    [HttpGet("total")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "Get coaches count",
        Description = "Returns total coach count. Admin scoped to own gym.",
        Tags = new[] { TagCoaches })]
    public async Task<IActionResult> GetCoachesTotal(CancellationToken ct)
    {
        var count = await _sender.Send(new GetCoachesCountQuery(), ct);
        return Ok(count);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Hard delete coach",
        Description = "Permanently deletes an inactive coach. OwnerApp only.",
        Tags = new[] { TagCoaches })]
    public async Task<IActionResult> DeleteCoach(Guid id, CancellationToken ct)
    {
        await _sender.Send(new DeleteCoachCommand(id), ct);
        return NoContent();
    }
}
