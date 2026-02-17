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
    // CREATE
    [HttpPost]
    public async Task<ActionResult<CreateWorkoutRoutineResponse>> Create(
        [FromBody] CreateWorkoutRoutineCommand command,
        [FromServices] CreateWorkoutRoutineHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(command, cancellationToken);
        return Ok(result);
    }

    // GET ALL active or inactive
    [HttpGet]
    public async Task<ActionResult<List<WorkoutRoutineListItem>>> Get(
     [FromQuery] RoutineStatus? status,
     [FromServices] GetWorkoutRoutinesHandler handler,
     CancellationToken cancellationToken)
    {
        var query = new GetWorkoutRoutinesQuery
        {
            Status = status
        };

        var result = await handler.Handle(query, cancellationToken);

        return Ok(result);
    }


    // GET BY ID
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WorkoutRoutineDetailsDto>> GetById(
        Guid id,
        [FromServices] GetWorkoutRoutineByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(id, cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    //Archive
    [HttpDelete("{id:guid}")]
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

    //Update
    [HttpPut("{id:guid}")]
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
    //Add exercises in a rutine

    [HttpPost("{id:guid}/exercises")]
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


    //Public Rutine
    [SwaggerOperation(Summary = "Public a rutine.")]
    [HttpPut("{id:guid}/publish")]
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
