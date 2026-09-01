using FitRos.Application.Features.Foods.ArchiveFood;
using FitRos.Application.Features.Foods.CreateFood;
using FitRos.Application.Features.Foods.GetFoodById;
using FitRos.Application.Features.Foods.GetFoods;
using FitRos.Application.Features.Foods.UpdateFood;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FitRos.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FoodsController : ControllerBase
{
    private readonly IMediator _mediator;

    public FoodsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // POST /api/foods
    [HttpPost]
    [ProducesResponseType(typeof(CreateFoodResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SwaggerOperation(
        Summary = "Create food",
        Description = "Creates a new food item in the catalog (macros per 100 g).")]
    public async Task<ActionResult<CreateFoodResponse>> Create(
        [FromBody] CreateFoodCommand command,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = response.Id },
            response);
    }

    // GET /api/foods/{id}
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FoodDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Get food by id",
        Description = "Retrieves a specific food item by its unique identifier.")]
    public async Task<ActionResult<FoodDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetFoodByIdQuery(id), cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    // GET /api/foods
    [HttpGet]
    [ProducesResponseType(typeof(List<FoodListItemDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "Get foods",
        Description = "Retrieves foods. Optionally filters by category and includes archived items.")]
    public async Task<IActionResult> Get(
        [FromQuery] FoodCategory? category,
        [FromQuery] bool includeArchived,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetFoodsQuery(category, includeArchived),
            cancellationToken);

        return Ok(result);
    }

    // PUT /api/foods/{id}
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Update food",
        Description = "Updates an existing food item.")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateFoodCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.Id)
            throw new DomainException("Route id does not match body id.");

        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }

    // DELETE /api/foods/{id}
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SwaggerOperation(
        Summary = "Archive food",
        Description = "Archives a food item (soft delete).")]
    public async Task<IActionResult> Archive(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new ArchiveFoodCommand(id), cancellationToken);

        return NoContent();
    }
}
