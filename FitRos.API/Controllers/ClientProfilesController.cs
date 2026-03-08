using FitRos.API.Contracts.ClientProfiles;
using FitRos.Application.Features.ClientProfiles.AddPhysicalMeasure;
using FitRos.Application.Features.ClientProfiles.ChangeClientStatus;
using FitRos.Application.Features.ClientProfiles.GetByCoach;
using FitRos.Application.Features.ClientProfiles.GetById;
using FitRos.Application.Features.ClientProfiles.GetMyClient;
using FitRos.Application.Features.ClientProfiles.GetMyClients;
using FitRos.Application.Features.ClientProfiles.HardDelete;
using FitRos.Application.Features.ClientProfiles.ReassignCoach;
using FitRos.Application.Features.ClientProfiles.SoftDelete;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FitRos.API.Controllers;

[ApiController]
[Route("api/client-profiles")]
[Authorize]
public sealed class ClientProfilesController : ControllerBase
{
    private const string TagClientProfiles = "ClientProfiles";

    private readonly IMediator _mediator;

    public ClientProfilesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ===============================
    // GET - My Clients (Coach)
    // ===============================

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MyClientListItemResponse>), StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "List my clients",
        Description = "Returns the list of clients owned by the authenticated coach.",
        Tags = new[] { TagClientProfiles })]
    public async Task<ActionResult<IReadOnlyList<MyClientListItemResponse>>> GetMyClients(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyClientsQuery(), cancellationToken);
        return Ok(result);
    }

    // ===============================
    // GET - Get by Id
    // ===============================

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClientDetailDto), StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "Get client by id",
        Description = "Returns full client detail including measures.",
        Tags = new[] { TagClientProfiles })]
    public async Task<ActionResult<ClientDetailDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetClientByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    // ===============================
    // GET - My client (Client role)
    // ===============================

    [HttpGet("me")]
    [ProducesResponseType(typeof(ClientDetailDto), StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "Get my client profile",
        Description = "Returns the authenticated client's profile.",
        Tags = new[] { TagClientProfiles })]
    public async Task<ActionResult<ClientDetailDto>> GetMyClient(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyClientsQuery(), cancellationToken);
        return Ok(result);
    }

    // ===============================
    // GET - By Coach (Admin)
    // ===============================

    [HttpGet("by-coach/{coachId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<ClientListItemDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "Get clients by coach",
        Description = "Returns clients assigned to a specific coach (Admin only).",
        Tags = new[] { TagClientProfiles })]
    public async Task<ActionResult<IReadOnlyList<ClientListItemDto>>> GetByCoach(
        Guid coachId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetClientsByCoachQuery(coachId), cancellationToken);
        return Ok(result);
    }

    // ===============================
    // POST - Add Physical Measure
    // ===============================

    [HttpPost("{id:guid}/measures")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerOperation(
        Summary = "Add physical measure",
        Description = "Adds a new physical measure to a client.",
        Tags = new[] { TagClientProfiles })]
    public async Task<IActionResult> AddPhysicalMeasure(
        Guid id,
        AddPhysicalMeasureCommand request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new AddPhysicalMeasureCommand(
                id,
                request.Weight,
                request.BodyFatPercentage,
                request.MuscleMass,
                request.Waist,
                request.Chest,
                request.Arms),
            cancellationToken);

        return NoContent();
    }

    // ===============================
    // PATCH - Change Status
    // ===============================
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerOperation(
        Summary = "Change client status",
        Description = "Activates or deactivates a client.",
        Tags = new[] { TagClientProfiles })]
    public async Task<IActionResult> ChangeStatus(
        Guid id,
        ChangeClientStatusRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new ToggleClientStatusCommand(id, request.Activate),
            cancellationToken);

        return NoContent();
    }

    // ===============================
    // PATCH - Reassign Coach (Admin)
    // ===============================

    [HttpPatch("{id:guid}/reassign")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerOperation(
        Summary = "Reassign client to another coach",
        Description = "Reassigns client ownership (Admin only).",
        Tags = new[] { TagClientProfiles })]
    public async Task<IActionResult> ReassignCoach(
        Guid id,
        ReassignClientCoachRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new ReassignClientCoachCommand(id, request.NewCoachId),
            cancellationToken);

        return NoContent();
    }

    // ===============================
    // DELETE - Soft Delete
    // ===============================

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerOperation(
        Summary = "Soft delete client",
        Description = "Marks a client as deleted.",
        Tags = new[] { TagClientProfiles })]
    public async Task<IActionResult> SoftDelete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new SoftDeleteClientCommand(id), cancellationToken);
        return NoContent();
    }

    // ===============================
    // DELETE - Hard Delete (Admin)
    // ===============================

    [HttpDelete("{id:guid}/hard")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerOperation(
        Summary = "Hard delete client",
        Description = "Permanently removes a client (Admin only).",
        Tags = new[] { TagClientProfiles })]
    public async Task<IActionResult> HardDelete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new HardDeleteClientCommand(id), cancellationToken);
        return NoContent();
    }
}