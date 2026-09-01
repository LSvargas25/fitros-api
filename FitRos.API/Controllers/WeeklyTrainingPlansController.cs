using FitRos.API.Contracts.WeeklyTrainingPlans;
using FitRos.Application.Features.WeeklyTrainingPlans.ActivatePlan;
using FitRos.Application.Features.WeeklyTrainingPlans.ArchivePlan;
using FitRos.Application.Features.WeeklyTrainingPlans.AssignRoutineToDay;
using FitRos.Application.Features.WeeklyTrainingPlans.CreatePlan;
using FitRos.Application.Features.WeeklyTrainingPlans.GetActivePlan;
using FitRos.Application.Features.WeeklyTrainingPlans.GetClientPlans;
using FitRos.Application.Features.WeeklyTrainingPlans.GetPlanById;
using FitRos.Application.Features.WeeklyTrainingPlans.RemoveRoutineFromDay;
using FitRos.Application.Features.WeeklyTrainingPlans.RenamePlan;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FitRos.API.Controllers;

[ApiController]
[Route("api/training-plans")]
[Authorize]
public sealed class WeeklyTrainingPlansController : ControllerBase
{
    private const string Tag = "WeeklyTrainingPlans";

    private readonly IMediator _mediator;

    public WeeklyTrainingPlansController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // POST /api/training-plans
    [HttpPost]
    [ProducesResponseType(typeof(CreateWeeklyTrainingPlanResponse), StatusCodes.Status201Created)]
    [SwaggerOperation(
        Summary = "Create training plan",
        Description = "Creates a new weekly training plan for a client.",
        Tags = new[] { Tag })]
    public async Task<ActionResult<CreateWeeklyTrainingPlanResponse>> Create(
        [FromBody] CreateWeeklyTrainingPlanCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    // GET /api/training-plans/{id}
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WeeklyTrainingPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Get training plan by ID",
        Description = "Returns the full training plan including assigned routines per day.",
        Tags = new[] { Tag })]
    public async Task<ActionResult<WeeklyTrainingPlanDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetWeeklyTrainingPlanByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    // GET /api/training-plans/client/{clientProfileId}
    [HttpGet("client/{clientProfileId:guid}")]
    [ProducesResponseType(typeof(List<TrainingPlanListItemDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "Get client training plans",
        Description = "Returns all training plans for a specific client.",
        Tags = new[] { Tag })]
    public async Task<ActionResult<List<TrainingPlanListItemDto>>> GetClientPlans(
        Guid clientProfileId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetClientPlansQuery(clientProfileId), cancellationToken);
        return Ok(result);
    }

    // GET /api/training-plans/client/{clientProfileId}/active
    [HttpGet("client/{clientProfileId:guid}/active")]
    [ProducesResponseType(typeof(WeeklyTrainingPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Get active training plan",
        Description = "Returns the currently active training plan for a client.",
        Tags = new[] { Tag })]
    public async Task<ActionResult<WeeklyTrainingPlanDto>> GetActivePlan(
        Guid clientProfileId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetActiveTrainingPlanQuery(clientProfileId), cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    // PATCH /api/training-plans/{id}/rename
    [HttpPatch("{id:guid}/rename")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Rename training plan",
        Description = "Updates the name of an existing training plan.",
        Tags = new[] { Tag })]
    public async Task<IActionResult> Rename(
        Guid id,
        [FromBody] RenamePlanRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new RenamePlanCommand(id, request.Name), cancellationToken);
        return NoContent();
    }

    // PATCH /api/training-plans/{id}/activate
    [HttpPatch("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Activate training plan",
        Description = "Sets the training plan status to Active.",
        Tags = new[] { Tag })]
    public async Task<IActionResult> Activate(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new ActivatePlanCommand(id), cancellationToken);
        return NoContent();
    }

    // PATCH /api/training-plans/{id}/archive
    [HttpPatch("{id:guid}/archive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Archive training plan",
        Description = "Archives the training plan, preventing further modifications.",
        Tags = new[] { Tag })]
    public async Task<IActionResult> Archive(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new ArchivePlanCommand(id), cancellationToken);
        return NoContent();
    }

    // PUT /api/training-plans/{id}/days/{day}
    [HttpPut("{id:guid}/days/{day:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Assign routine to day",
        Description = "Assigns or replaces a workout routine for a specific day (0=Sunday … 6=Saturday).",
        Tags = new[] { Tag })]
    public async Task<IActionResult> AssignRoutineToDay(
        Guid id,
        int day,
        [FromBody] AssignRoutineToDayRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new AssignRoutineToDayCommand(id, (DayOfWeek)day, request.WorkoutRoutineId, request.Notes),
            cancellationToken);
        return NoContent();
    }

    // DELETE /api/training-plans/{id}/days/{day}
    [HttpDelete("{id:guid}/days/{day:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Remove routine from day",
        Description = "Removes the assigned workout routine from a specific day of the week.",
        Tags = new[] { Tag })]
    public async Task<IActionResult> RemoveRoutineFromDay(
        Guid id,
        int day,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new RemoveRoutineFromDayCommand(id, (DayOfWeek)day), cancellationToken);
        return NoContent();
    }
}
