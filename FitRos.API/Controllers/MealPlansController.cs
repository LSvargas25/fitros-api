using FitRos.API.Contracts.MealPlans;
using FitRos.Application.Features.MealPlans.ActivateMealPlan;
using FitRos.Application.Features.MealPlans.AddMealPlanEntry;
using FitRos.Application.Features.MealPlans.ArchiveMealPlan;
using FitRos.Application.Features.MealPlans.CreateMealPlan;
using FitRos.Application.Features.MealPlans.GetActiveMealPlan;
using FitRos.Application.Features.MealPlans.GetClientMealPlans;
using FitRos.Application.Features.MealPlans.GetMealPlanById;
using FitRos.Application.Features.MealPlans.RemoveMealPlanEntry;
using FitRos.Application.Features.MealPlans.RenameMealPlan;
using FitRos.Application.Features.MealPlans.UpdateMealPlanEntry;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FitRos.API.Controllers;

[ApiController]
[Route("api/meal-plans")]
[Authorize]
public sealed class MealPlansController : ControllerBase
{
    private const string Tag = "MealPlans";

    private readonly IMediator _mediator;

    public MealPlansController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // POST /api/meal-plans
    [HttpPost]
    [ProducesResponseType(typeof(CreateMealPlanResponse), StatusCodes.Status201Created)]
    [SwaggerOperation(
        Summary = "Create meal plan",
        Description = "Creates a new meal plan and assigns it to a client.",
        Tags = new[] { Tag })]
    public async Task<ActionResult<CreateMealPlanResponse>> Create(
        [FromBody] CreateMealPlanCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    // GET /api/meal-plans/{id}
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MealPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Get meal plan by ID",
        Description = "Returns the full meal plan including per-day / per-meal food entries and macro totals.",
        Tags = new[] { Tag })]
    public async Task<ActionResult<MealPlanDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMealPlanByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    // GET /api/meal-plans/client/{clientProfileId}
    [HttpGet("client/{clientProfileId:guid}")]
    [ProducesResponseType(typeof(List<MealPlanListItemDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "Get client meal plans",
        Description = "Returns all meal plans for a specific client.",
        Tags = new[] { Tag })]
    public async Task<ActionResult<List<MealPlanListItemDto>>> GetClientPlans(
        Guid clientProfileId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetClientMealPlansQuery(clientProfileId), cancellationToken);
        return Ok(result);
    }

    // GET /api/meal-plans/client/{clientProfileId}/active
    [HttpGet("client/{clientProfileId:guid}/active")]
    [ProducesResponseType(typeof(MealPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Get active meal plan",
        Description = "Returns the currently active meal plan for a client.",
        Tags = new[] { Tag })]
    public async Task<ActionResult<MealPlanDto>> GetActivePlan(
        Guid clientProfileId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetActiveMealPlanQuery(clientProfileId), cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    // PATCH /api/meal-plans/{id}/rename
    [HttpPatch("{id:guid}/rename")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(Summary = "Rename meal plan", Tags = new[] { Tag })]
    public async Task<IActionResult> Rename(
        Guid id,
        [FromBody] RenameMealPlanRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new RenameMealPlanCommand(id, request.Name), cancellationToken);
        return NoContent();
    }

    // PATCH /api/meal-plans/{id}/activate
    [HttpPatch("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(Summary = "Activate meal plan", Tags = new[] { Tag })]
    public async Task<IActionResult> Activate(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new ActivateMealPlanCommand(id), cancellationToken);
        return NoContent();
    }

    // PATCH /api/meal-plans/{id}/archive
    [HttpPatch("{id:guid}/archive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(Summary = "Archive meal plan", Tags = new[] { Tag })]
    public async Task<IActionResult> Archive(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new ArchiveMealPlanCommand(id), cancellationToken);
        return NoContent();
    }

    // POST /api/meal-plans/{id}/entries
    [HttpPost("{id:guid}/entries")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Add entry to meal plan",
        Description = "Adds a food to a (day, meal) slot. Re-adding the same food in the same slot replaces its quantity.",
        Tags = new[] { Tag })]
    public async Task<IActionResult> AddEntry(
        Guid id,
        [FromBody] AddMealPlanEntryRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new AddMealPlanEntryCommand(id, request.Day, request.Meal, request.FoodId, request.QuantityGrams),
            cancellationToken);
        return NoContent();
    }

    // PUT /api/meal-plans/{id}/entries/{entryId}
    [HttpPut("{id:guid}/entries/{entryId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(Summary = "Update a meal plan entry's quantity", Tags = new[] { Tag })]
    public async Task<IActionResult> UpdateEntry(
        Guid id,
        Guid entryId,
        [FromBody] UpdateMealPlanEntryRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new UpdateMealPlanEntryCommand(id, entryId, request.QuantityGrams),
            cancellationToken);
        return NoContent();
    }

    // DELETE /api/meal-plans/{id}/entries/{entryId}
    [HttpDelete("{id:guid}/entries/{entryId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(Summary = "Remove an entry from a meal plan", Tags = new[] { Tag })]
    public async Task<IActionResult> RemoveEntry(
        Guid id,
        Guid entryId,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new RemoveMealPlanEntryCommand(id, entryId), cancellationToken);
        return NoContent();
    }
}
