using FitRos.Application.Features.Exercises.CreateExercise;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FitRos.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExercisesController : ControllerBase
{
    private readonly CreateExerciseHandler _handler;

    public ExercisesController(CreateExerciseHandler handler)
    {
        _handler = handler;
    }

    /// Creates a new exercise.
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SwaggerOperation(
        Summary = "Create exercise",
        Description = "Creates a new exercise in the system."
    )]
    public async Task<IActionResult> Create(
        [FromBody] CreateExerciseCommand command,
        CancellationToken cancellationToken)
    {
        await _handler.Handle(command, cancellationToken);

        return StatusCode(StatusCodes.Status201Created);
    }
}
