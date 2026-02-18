using FitRos.Application.Features.WorkoutRoutines.AddExerciseToWorkoutRoutine;
using FitRos.Application.Features.WorkoutRoutines.ArchiveWorkoutRoutine;
using FitRos.Application.Features.WorkoutRoutines.CreateWorkoutRoutine;
using FitRos.Application.Features.WorkoutRoutines.GetWorkoutRoutineById;
using FitRos.Application.Features.WorkoutRoutines.GetWorkoutRoutines;
using FitRos.Application.Features.WorkoutRoutines.PublishWorkoutRoutine;
using FitRos.Application.Features.WorkoutRoutines.UpdateWorkoutRoutine;
using FitRos.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FitRos.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkoutRoutinesController : ControllerBase
{
    /// Creates a new workout routine.
    [HttpPost]
    [ProducesResponseType(typeof(CreateWorkoutRoutineResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SwaggerOperation(
        Summary = "Create workout routine",
        Description = "Creates a new workout routine in Draft status."
    )]
    public async Task<ActionResult<CreateWorkoutRoutineResponse>> Create(
        [FromBody] CreateWorkoutRoutineCommand command,
        [FromServices] CreateWorkoutRoutineHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// Retrieves all workout routines filtered by status.
    [HttpGet]
    [ProducesResponseType(typeof(List<WorkoutRoutineListItem>), StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "Get workout routines",
        Description = "Retrieves all workout routines optionally filtered by status (Draft, Published, Archived)."
    )]
    public async Task<ActionResult<List<WorkoutRoutineListItem>>> Get(
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
        Description = "Retrieves detailed information about a specific workout routine."
    )]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromServices] GetWorkoutRoutineByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var query = new GetWorkoutRoutineByIdQuery(id);

        var result = await handler.Handle(query, cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    /// Archives (soft deletes) a workout routine.
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Archive workout routine",
        Description = "Archives a workout routine, changing its status to Archived."
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

    /// Updates the basic information of a workout routine.
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Update workout routine",
        Description = "Updates the name and description of an existing workout routine."
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

    /// Adds an exercise to a specific workout routine.
    [HttpPost("{id:guid}/exercises")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Add exercise to workout routine",
        Description = "Adds a new exercise to the specified workout routine."
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

    /// Publishes a workout routine.
    [HttpPut("{id:guid}/publish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Publish workout routine",
        Description = "Changes the workout routine status from Draft to Published."
    )]
    public async Task<IActionResult> Publish(
        Guid id,
        [FromServices] PublishWorkoutRoutineHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new PublishWorkoutRoutineCommand(id),
            cancellationToken);

        if (!result)
            return NotFound();

        return NoContent();
    }
}
