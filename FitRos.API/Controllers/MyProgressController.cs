using FitRos.API.Contracts.ClientProfiles;
using FitRos.Application.Features.ClientProfiles.GetPhysicalMeasures;
using FitRos.Application.Features.ClientProfiles.ProgressReport.Common;
using FitRos.Application.Features.MyProgress.AddMyMeasurement;
using FitRos.Application.Features.MyProgress.GetMyMeasurements;
using FitRos.Application.Features.MyProgress.GetMyProgressReport;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FitRos.API.Controllers;

/// <summary>
/// Self-facing body-measurement and progress endpoints. Everything is scoped to
/// the caller's ClientProfile (resolved from the JWT, never a supplied id).
/// Available to Client and to Coach (a coach has a personal CoachSelf profile —
/// see BACKEND_AUDIT §19 / §26). Staff manage other people's data through
/// /api/client-profiles/{id}/...
/// </summary>
[ApiController]
[Route("api/my")]
[Authorize(Roles = "Client,Coach")]
public sealed class MyProgressController : ControllerBase
{
    private const string Tag = "My Progress";

    private readonly ISender _sender;

    public MyProgressController(ISender sender)
    {
        _sender = sender;
    }

    // GET /api/my/measurements
    [HttpGet("measurements")]
    [ProducesResponseType(typeof(List<PhysicalMeasureHistoryDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "Get my body-measurement history",
        Description = "The authenticated user's own physical measures, most recent first.",
        Tags = new[] { Tag })]
    public async Task<ActionResult<List<PhysicalMeasureHistoryDto>>> GetMyMeasurements(
        CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetMyMeasurementsQuery(), cancellationToken));

    // POST /api/my/measurements
    [HttpPost("measurements")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerOperation(
        Summary = "Record one of my body measurements",
        Description = "Records a measurement for the authenticated user. RecordedAt is optional " +
                      "(defaults to now; may be back-dated up to 60 days, never future).",
        Tags = new[] { Tag })]
    public async Task<IActionResult> AddMyMeasurement(
        [FromBody] AddPhysicalMeasureRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(
            new AddMyMeasurementCommand(
                request.Weight,
                request.BodyFatPercentage,
                request.MuscleMass,
                request.Waist,
                request.Chest,
                request.Arms,
                request.RecordedAt),
            cancellationToken);
        return NoContent();
    }

    // GET /api/my/progress-report
    [HttpGet("progress-report")]
    [ProducesResponseType(typeof(ProgressReportDto), StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "Get my monthly progress report",
        Description = "Weight, body-fat %, waist and completed-set counts for the given month vs " +
                      "the previous month. Omit year/month for the current UTC month. Nothing persisted.",
        Tags = new[] { Tag })]
    public async Task<ActionResult<ProgressReportDto>> GetMyProgressReport(
        [FromQuery] int? year,
        [FromQuery] int? month,
        CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetMyProgressReportQuery(year, month), cancellationToken));
}
