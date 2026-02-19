using FitRos.Application.Features.Exercises.ArchiveExercise;
using FitRos.Application.Features.Exercises.CreateExercise;
using FitRos.Application.Features.Exercises.GetExerciseById;
using FitRos.Application.Features.Exercises.GetExercises;
using FitRos.Application.Features.Exercises.UpdateExercise;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Enums;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FitRos.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExercisesController : ControllerBase
{
    private readonly CreateExerciseHandler _createHandler;
    private readonly GetExerciseByIdHandler _getByIdHandler;
    private readonly GetExercisesHandler _getHandler;
    private readonly UpdateExerciseHandler _updateHandler;
    private readonly ArchiveExerciseHandler _archiveHandler;



    public ExercisesController(
        CreateExerciseHandler createHandler,
        GetExerciseByIdHandler getByIdHandler,
        GetExercisesHandler getHandler,
        UpdateExerciseHandler updateHandler,
        ArchiveExerciseHandler archiveExerciseHandler)
    {
        _createHandler = createHandler;
        _getByIdHandler = getByIdHandler;
        _getHandler = getHandler;
        _updateHandler = updateHandler;
        _archiveHandler = archiveExerciseHandler;
    }

    // POST
    [HttpPost]
    [ProducesResponseType(typeof(CreateExerciseResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SwaggerOperation(
        Summary = "Create exercise",
        Description = "Creates a new exercise in the system."
    )]
    public async Task<ActionResult<CreateExerciseResponse>> Create(
        [FromBody] CreateExerciseCommand command,
        CancellationToken cancellationToken)
    {
        var response = await _createHandler.Handle(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = response.Id },
            response
        );
    }
    // GET BY ID
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ExerciseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Get exercise by id",
        Description = "Retrieves a specific exercise by its unique identifier."
    )]
    public async Task<ActionResult<ExerciseDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _getByIdHandler.Handle(id, cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }


    // GET ALL
    [HttpGet]
    [ProducesResponseType(typeof(List<ExerciseListItemDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "Get exercises",
        Description = "Retrieves all active exercises. Optionally filters the results by muscle group category using the 'category' query parameter."
    )]
    public async Task<IActionResult> Get(
        [FromQuery] MuscleGroup? category,
        CancellationToken cancellationToken)
    {
        var result = await _getHandler.Handle(
            new GetExercisesQuery(category),
            cancellationToken);

        return Ok(result);
    }


    // UPDATE
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Update exercise",
        Description = "Updates an existing exercise."
    )]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateExerciseCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.Id)
            throw new DomainException("Route id does not match body id.");

        await _updateHandler.Handle(command, cancellationToken);

        return NoContent();
    }
    // ARCHIVE
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SwaggerOperation(
        Summary = "Archive exercise",
        Description = "Archives an existing exercise (soft delete)."
    )]
    public async Task<IActionResult> Archive(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _archiveHandler.Handle(
            new ArchiveExerciseCommand(id),
            cancellationToken);

        return NoContent();
    }


}
