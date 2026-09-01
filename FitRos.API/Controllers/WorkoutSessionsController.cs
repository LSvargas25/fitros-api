using FitRos.API.Contracts.WorkoutSessions;
using FitRos.Application.Features.WorkoutSessions;
using FitRos.Application.Features.WorkoutSessions.AddSetToWorkoutSession;
using FitRos.Application.Features.WorkoutSessions.CompleteWorkoutSession;
using FitRos.Application.Features.WorkoutSessions.GetMyWorkoutSessionHistory;
using FitRos.Application.Features.WorkoutSessions.GetWorkoutSessionById;
using FitRos.Application.Features.WorkoutSessions.RemoveSetFromWorkoutSession;
using FitRos.Application.Features.WorkoutSessions.SkipWorkoutSession;
using FitRos.Application.Features.WorkoutSessions.StartTodaysWorkoutSession;
using FitRos.Application.Features.WorkoutSessions.UpdateSetInWorkoutSession;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FitRos.API.Controllers;

[ApiController]
[Route("api/workout-sessions")]
[Authorize]
public sealed class WorkoutSessionsController : ControllerBase
{
    private const string Tag = "WorkoutSessions";

    private readonly IMediator _mediator;

    public WorkoutSessionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // POST /api/workout-sessions/start-today
    [HttpPost("start-today")]
    [ProducesResponseType(typeof(WorkoutSessionDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Start (or resume) today's workout session",
        Description = "Starts a workout session for today based on the client's active weekly plan, " +
                      "or the given routine id if provided. Returns the existing session if one " +
                      "already exists for today.",
        Tags = new[] { Tag })]
    public async Task<ActionResult<WorkoutSessionDetailsDto>> StartToday(
        [FromBody] StartWorkoutSessionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new StartTodaysWorkoutSessionCommand(request.RoutineId),
            cancellationToken);

        return Ok(result);
    }

    // GET /api/workout-sessions/history
    [HttpGet("history")]
    [ProducesResponseType(typeof(List<WorkoutSessionListItemDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "Get my workout session history",
        Description = "Returns the authenticated client's past workout sessions, most recent first. " +
                      "Defaults to the last month.",
        Tags = new[] { Tag })]
    public async Task<ActionResult<List<WorkoutSessionListItemDto>>> GetHistory(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyWorkoutSessionHistoryQuery(from, to), cancellationToken);
        return Ok(result);
    }

    // GET /api/workout-sessions/{id}
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WorkoutSessionDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Get workout session by id",
        Description = "Returns a workout session with its logged sets and the routine's suggested sets/reps.",
        Tags = new[] { Tag })]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetWorkoutSessionByIdQuery(id), cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    // POST /api/workout-sessions/{id}/sets
    [HttpPost("{id:guid}/sets")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Log a set",
        Description = "Registers the reps and weight used for one set of one exercise in the session.",
        Tags = new[] { Tag })]
    public async Task<IActionResult> AddSet(
        Guid id,
        [FromBody] AddSetRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new AddSetToWorkoutSessionCommand(
                id,
                request.ExerciseId,
                request.SetNumber,
                request.RepsAchieved,
                request.WeightUsed),
            cancellationToken);

        return NoContent();
    }

    // PUT /api/workout-sessions/{id}/sets/{setId}
    [HttpPut("{id:guid}/sets/{setId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Edit a logged set",
        Description = "Corrects the set number, reps, or weight of an already-logged set.",
        Tags = new[] { Tag })]
    public async Task<IActionResult> UpdateSet(
        Guid id,
        Guid setId,
        [FromBody] UpdateSetRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new UpdateSetInWorkoutSessionCommand(
                id,
                setId,
                request.SetNumber,
                request.RepsAchieved,
                request.WeightUsed),
            cancellationToken);

        return NoContent();
    }

    // DELETE /api/workout-sessions/{id}/sets/{setId}
    [HttpDelete("{id:guid}/sets/{setId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Remove a logged set",
        Description = "Deletes a mistakenly logged set from the session.",
        Tags = new[] { Tag })]
    public async Task<IActionResult> RemoveSet(
        Guid id,
        Guid setId,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new RemoveSetFromWorkoutSessionCommand(id, setId), cancellationToken);
        return NoContent();
    }

    // PATCH /api/workout-sessions/{id}/complete
    [HttpPatch("{id:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerOperation(
        Summary = "Complete a workout session",
        Description = "Marks the session as completed. Requires at least one logged set.",
        Tags = new[] { Tag })]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new CompleteWorkoutSessionCommand(id), cancellationToken);
        return NoContent();
    }

    // PATCH /api/workout-sessions/{id}/skip
    [HttpPatch("{id:guid}/skip")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerOperation(
        Summary = "Skip a workout session",
        Description = "Marks the session as skipped (not trained).",
        Tags = new[] { Tag })]
    public async Task<IActionResult> Skip(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new SkipWorkoutSessionCommand(id), cancellationToken);
        return NoContent();
    }
}
