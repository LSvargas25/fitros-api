using FitRos.API.Contracts.ClientProfiles;
using FitRos.Application.Features.ClientProfiles.ActivateClientInGym;
using FitRos.Application.Features.ClientProfiles.AddPhysicalMeasure;
using FitRos.Application.Features.ClientProfiles.ChangeClientStatus;
using FitRos.Application.Features.ClientProfiles.GenerateKpiSnapshot;
using FitRos.Application.Features.ClientProfiles.GetByCoach;
using FitRos.Application.Features.ClientProfiles.GetById;
using FitRos.Application.Features.ClientProfiles.GetKpiSnapshots;
using FitRos.Application.Features.ClientProfiles.GetMyClient;
using FitRos.Application.Features.ClientProfiles.GetMyClientProfile;
using FitRos.Application.Features.ClientProfiles.GetMyClients;
using FitRos.Application.Features.ClientProfiles.GetPhysicalMeasures;
using FitRos.Application.Features.ClientProfiles.HardDelete;
using FitRos.Application.Features.ClientProfiles.ProgressReport.Common;
using FitRos.Application.Features.ClientProfiles.ProgressReport.GenerateProgressReport;
using FitRos.Application.Features.ClientProfiles.ProgressReport.GetProgressReport;
using FitRos.Application.Features.ClientProfiles.ProgressReport.GetProgressReportHistory;
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
        var result = await _mediator.Send(new GetMyClientProfileQuery(), cancellationToken);
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
        [FromBody] AddPhysicalMeasureRequest request,
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
    // GET - Physical Measure History
    // ===============================

    [HttpGet("{id:guid}/measures")]
    [ProducesResponseType(typeof(List<PhysicalMeasureHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Get a client's physical measure history",
        Description = "Returns the client's physical measures, most recent first.",
        Tags = new[] { TagClientProfiles })]
    public async Task<ActionResult<List<PhysicalMeasureHistoryDto>>> GetPhysicalMeasures(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPhysicalMeasuresQuery(id), cancellationToken);
        return Ok(result);
    }

    // ===============================
    // POST - Generate KPI Snapshot
    // ===============================

    [HttpPost("{id:guid}/kpi-snapshot")]
    [ProducesResponseType(typeof(GenerateClientKpiSnapshotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Generate a KPI snapshot",
        Description = "Computes weight/body-fat/waist deltas between the client's two most recent " +
                      "physical measures and persists the result as a KPI snapshot.",
        Tags = new[] { TagClientProfiles })]
    public async Task<ActionResult<GenerateClientKpiSnapshotResponse>> GenerateKpiSnapshot(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GenerateClientKpiSnapshotCommand(id), cancellationToken);
        return Ok(result);
    }

    // ===============================
    // GET - KPI Snapshot History
    // ===============================

    [HttpGet("{id:guid}/kpi-snapshots")]
    [ProducesResponseType(typeof(List<ClientKpiSnapshotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Get a client's KPI snapshot history",
        Description = "Returns the client's KPI snapshots, most recent first.",
        Tags = new[] { TagClientProfiles })]
    public async Task<ActionResult<List<ClientKpiSnapshotDto>>> GetKpiSnapshots(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetClientKpiSnapshotsQuery(id), cancellationToken);
        return Ok(result);
    }

    // ===============================
    // GET - Monthly Progress Report (computed on the fly)
    // ===============================

    [HttpGet("{id:guid}/progress-report")]
    [ProducesResponseType(typeof(ProgressReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Get a client's monthly progress report",
        Description = "Weight, body-fat %, waist and completed-set counts for the given month vs the " +
                      "previous month, computed live from PhysicalMeasure and completed WorkoutSessions. " +
                      "Omit year/month for the current month. Nothing is persisted.",
        Tags = new[] { TagClientProfiles })]
    public async Task<ActionResult<ProgressReportDto>> GetProgressReport(
        Guid id,
        [FromQuery] int? year,
        [FromQuery] int? month,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetProgressReportQuery(id, year, month), cancellationToken);
        return Ok(result);
    }

    // ===============================
    // POST - Generate & persist a Progress Report snapshot
    // ===============================

    [HttpPost("{id:guid}/progress-report")]
    [ProducesResponseType(typeof(GenerateProgressReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Generate a progress-report snapshot",
        Description = "Computes the monthly progress report and freezes it as a " +
                      "ClientProgressReportSnapshot (ReportJson), anchored to a PhysicalMeasure. " +
                      "Omit year/month for the current month.",
        Tags = new[] { TagClientProfiles })]
    public async Task<ActionResult<GenerateProgressReportResponse>> GenerateProgressReport(
        Guid id,
        [FromQuery] int? year,
        [FromQuery] int? month,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GenerateProgressReportCommand(id, year, month), cancellationToken);
        return Ok(result);
    }

    // ===============================
    // GET - Progress Report snapshot history
    // ===============================

    [HttpGet("{id:guid}/progress-reports")]
    [ProducesResponseType(typeof(List<ProgressReportSnapshotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Get a client's persisted progress-report snapshots",
        Description = "Returns the client's generated progress-report snapshots, most recent first.",
        Tags = new[] { TagClientProfiles })]
    public async Task<ActionResult<List<ProgressReportSnapshotDto>>> GetProgressReportHistory(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetProgressReportHistoryQuery(id), cancellationToken);
        return Ok(result);
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
        [FromBody] ChangeClientStatusRequest request,
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
        [FromBody] ReassignClientCoachRequest request,
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
    // PATCH - Activate in Gym (transfer)
    // ===============================

    [HttpPatch("{id:guid}/activate-in-gym")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerOperation(
        Summary = "Activate client in gym",
        Description = "Transfers an inactive client to a gym and activates them. OwnerApp or Admin.",
        Tags = new[] { TagClientProfiles })]
    public async Task<IActionResult> ActivateInGym(
        Guid id,
        [FromBody] ActivateClientInGymRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new ActivateClientInGymCommand(id, request.GymId),
            cancellationToken);

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