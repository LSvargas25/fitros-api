using FitRos.API.Contracts.WorkoutRoutines;
using FitRos.Application.Common.Models;
using FitRos.Application.Features.WorkoutRoutines.AddExerciseToWorkoutRoutine;
using FitRos.Application.Features.WorkoutRoutines.ArchiveWorkoutRoutine;
using FitRos.Application.Features.WorkoutRoutines.CreateWorkoutRoutine;
using FitRos.Application.Features.WorkoutRoutines.CreateWorkoutRoutineVersion;
using FitRos.Application.Features.WorkoutRoutines.GetLatestWorkoutRoutine;
using FitRos.Application.Features.WorkoutRoutines.GetWorkoutRoutineById;
using FitRos.Application.Features.WorkoutRoutines.GetWorkoutRoutines;
using FitRos.Application.Features.WorkoutRoutines.GetWorkoutRoutineVersions;
using FitRos.Application.Features.WorkoutRoutines.MoveExerciseInWorkoutRoutine;
using FitRos.Application.Features.WorkoutRoutines.PublishWorkoutRoutine;
using FitRos.Application.Features.WorkoutRoutines.RemoveExerciseFromWorkoutRoutine;
using FitRos.Application.Features.WorkoutRoutines.UpdateExerciseInWorkoutRoutine;
using FitRos.Application.Features.WorkoutRoutines.UpdateWorkoutRoutine;
using FitRos.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FitRos.API.Controllers;

[ApiController]
[Route("api/[controller]")]
// These actions call their handlers directly (not via MediatR), so the MediatR
// Authentication/Authorization pipeline behaviors never run for them. Enforce
// auth at the controller: a valid JWT is required and Clients are excluded
// (routine authoring is gym-staff only).
[Authorize(Roles = "OwnerApp,Admin,Coach")]
public class WorkoutRoutinesController : ControllerBase
{
    private const string TagCore = "WorkoutRoutines - Core";
    private const string TagVersioning = "WorkoutRoutines - Versioning";
    private const string TagLifecycle = "WorkoutRoutines - Lifecycle";
    private const string TagExercises = "WorkoutRoutines - Exercises";

    // =========================================================
    // Core CRUD (GET → POST → PUT)
    // =========================================================

    /// Retrieves a page of workout routines filtered by status.
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<WorkoutRoutineListItem>), StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "Get workout routines",
        Description = "Retrieves a paged envelope ({ page, pageSize, totalCount, items }) of workout " +
                      "routines, optionally filtered by status (Draft, Published, Archived).",
        Tags = new[] { TagCore }
    )]
    public async Task<ActionResult<PagedResult<WorkoutRoutineListItem>>> Get(
        [FromQuery] RoutineStatus? status,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromServices] GetWorkoutRoutinesHandler handler,
        CancellationToken cancellationToken)
    {
        var query = new GetWorkoutRoutinesQuery(
            status,
            page == 0 ? 1 : page,
            pageSize == 0 ? 10 : pageSize
        );

        var result = await handler.Handle(query, cancellationToken);
        return Ok(result);
    }

    /// Retrieves a workout routine by its identifier.
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WorkoutRoutineDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Get workout routine by id",
        Description = "Retrieves detailed information about a specific workout routine.",
        Tags = new[] { TagCore }
    )]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromServices] GetWorkoutRoutineByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new GetWorkoutRoutineByIdQuery(id),
            cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    /// Creates a new workout routine.
    [HttpPost]
    [ProducesResponseType(typeof(CreateWorkoutRoutineResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SwaggerOperation(
        Summary = "Create workout routine",
        Description = "Creates a new workout routine in Draft status.",
        Tags = new[] { TagCore }
    )]
    public async Task<ActionResult<CreateWorkoutRoutineResponse>> Create(
        [FromBody] CreateWorkoutRoutineCommand command,
        [FromServices] CreateWorkoutRoutineHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// Updates the basic information of a workout routine.
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Update workout routine",
        Description = "Updates the name and description of an existing workout routine.",
        Tags = new[] { TagCore }
    )]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateWorkoutRoutineCommand command,
        [FromServices] UpdateWorkoutRoutineHandler handler,
        CancellationToken cancellationToken)
    {
        var updated = await handler.Handle(id, command, cancellationToken);

        if (!updated)
            return NotFound();

        return NoContent();
    }

    // =========================================================
    // Versioning (GET → POST)
    // =========================================================

    /// Retrieves the latest version within the routine group.
    [HttpGet("{id:guid}/latest")]
    [ProducesResponseType(typeof(WorkoutRoutineDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Get latest workout routine version",
        Description = "Retrieves the most recent version within the routine group of the specified routine.",
        Tags = new[] { TagVersioning }
    )]
    public async Task<IActionResult> GetLatest(
        Guid id,
        [FromServices] GetLatestWorkoutRoutineHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new GetLatestWorkoutRoutineQuery(id),
            cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    /// Retrieves all versions for the routine group.
    [HttpGet("{id:guid}/versions")]
    [ProducesResponseType(typeof(List<WorkoutRoutineVersionListItem>), StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "Get workout routine versions",
        Description = "Retrieves all versions for the routine group ordered by Version descending.",
        Tags = new[] { TagVersioning }
    )]
    public async Task<ActionResult<List<WorkoutRoutineVersionListItem>>> GetVersions(
        Guid id,
        [FromServices] GetWorkoutRoutineVersionsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new GetWorkoutRoutineVersionsQuery(id),
            cancellationToken);

        return Ok(result);
    }

    /// Creates a new draft version from a published routine.
    [HttpPost("{id:guid}/versions")]
    [ProducesResponseType(typeof(CreateWorkoutRoutineVersionResponse), StatusCodes.Status201Created)]
    [SwaggerOperation(
        Summary = "Create workout routine version",
        Description = "Creates a new Draft version from a Published workout routine.",
        Tags = new[] { TagVersioning }
    )]
    public async Task<ActionResult<CreateWorkoutRoutineVersionResponse>> CreateVersion(
        Guid id,
        [FromServices] CreateWorkoutRoutineVersionHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new CreateWorkoutRoutineVersionCommand(id),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    // =========================================================
    // Lifecycle (PUT → DELETE)
    // =========================================================

    /// Publishes a workout routine.
    [HttpPut("{id:guid}/publish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerOperation(
        Summary = "Publish workout routine",
        Description = "Publishes a Draft workout routine.",
        Tags = new[] { TagLifecycle }
    )]
    public async Task<IActionResult> Publish(
        Guid id,
        [FromServices] PublishWorkoutRoutineHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.Handle(
            new PublishWorkoutRoutineCommand(id),
            cancellationToken);

        return NoContent();
    }

    /// Archives a workout routine.
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerOperation(
        Summary = "Archive workout routine",
        Description = "Archives a Published workout routine.",
        Tags = new[] { TagLifecycle }
    )]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] ArchiveWorkoutRoutineHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new ArchiveWorkoutRoutineCommand(id),
            cancellationToken);

        if (!result)
            return NotFound();

        return NoContent();
    }

    // =========================================================
    // Exercises (POST → PUT → DELETE)
    // =========================================================

    /// Adds an exercise to a workout routine.
    [HttpPost("{id:guid}/exercises")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerOperation(
        Summary = "Add exercise to workout routine",
        Description = "Adds a new exercise to the specified routine.",
        Tags = new[] { TagExercises }
    )]
    public async Task<IActionResult> AddExercise(
        Guid id,
        [FromBody] AddExerciseToWorkoutRoutineCommand command,
        [FromServices] AddExerciseToWorkoutRoutineHandler handler,
        CancellationToken cancellationToken)
    {
        if (id != command.WorkoutRoutineId)
            return BadRequest();

        var result = await handler.Handle(command, cancellationToken);

        if (!result)
            return NotFound();

        return NoContent();
    }

    /// Updates the target sets / reps / rest of an exercise already in a routine.
    [HttpPut("{routineId:guid}/exercises/{exerciseId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Update an exercise's targets in a workout routine",
        Description = "Changes suggested sets / reps / rest for an exercise already in a Draft routine, " +
                      "keeping its position. Only Draft routines can be modified.",
        Tags = new[] { TagExercises }
    )]
    public async Task<IActionResult> UpdateExercise(
        Guid routineId,
        Guid exerciseId,
        [FromBody] UpdateRoutineExerciseRequest request,
        [FromServices] UpdateExerciseInWorkoutRoutineHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateExerciseInWorkoutRoutineCommand(
            routineId,
            exerciseId,
            request.SuggestedSets,
            request.SuggestedReps,
            request.SuggestedRestSeconds);

        await handler.Handle(command, cancellationToken);

        return NoContent();
    }

    /// Moves an exercise inside a routine.
    [HttpPut("{routineId:guid}/exercises/{exerciseId:guid}/move")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerOperation(
        Summary = "Move exercise in workout routine",
        Description = "Moves an exercise to a new position within a draft routine.",
        Tags = new[] { TagExercises }
    )]
    public async Task<IActionResult> MoveExercise(
        Guid routineId,
        Guid exerciseId,
        [FromBody] MoveExerciseRequest request,
        [FromServices] MoveExerciseInWorkoutRoutineHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new MoveExerciseInWorkoutRoutineCommand(
            routineId,
            exerciseId,
            request.NewOrder);

        await handler.Handle(command, cancellationToken);

        return NoContent();
    }

    /// Removes an exercise from a routine.
    [HttpDelete("{routineId:guid}/exercises/{exerciseId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerOperation(
        Summary = "Remove exercise from workout routine",
        Description = "Removes an exercise from a draft routine.",
        Tags = new[] { TagExercises }
    )]
    public async Task<IActionResult> RemoveExercise(
        Guid routineId,
        Guid exerciseId,
        [FromServices] RemoveExerciseFromWorkoutRoutineHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new RemoveExerciseFromWorkoutRoutineCommand(
            routineId,
            exerciseId);

        await handler.Handle(command, cancellationToken);

        return NoContent();
    }
}