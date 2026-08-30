using FitRos.Application.Features.MealPlans.GetClientMealPlans;
using FitRos.Application.Features.MealPlans.GetMealPlanById;
using FitRos.Application.Features.MyPlans.GetMyMealPlanById;
using FitRos.Application.Features.MyPlans.GetMyMealPlans;
using FitRos.Application.Features.MyPlans.GetMyTrainingPlanById;
using FitRos.Application.Features.MyPlans.GetMyTrainingPlans;
using FitRos.Application.Features.WeeklyTrainingPlans.GetClientPlans;
using FitRos.Application.Features.WeeklyTrainingPlans.GetPlanById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FitRos.API.Controllers;

/// <summary>
/// Read-only, self-facing views of the authenticated user's own plans.
/// Everything is scoped to the caller's ClientProfile (resolved from the JWT),
/// never to an id supplied by the caller. Available to Client and to Coach (a
/// coach has a personal ClientProfile of their own — see §19 in BACKEND_AUDIT);
/// Admin/Owner get 403. Staff manage other people's plans through the
/// /api/meal-plans and /api/training-plans controllers.
/// </summary>
[ApiController]
[Route("api/my")]
[Authorize(Roles = "Client,Coach")]
public sealed class MyPlansController : ControllerBase
{
    private const string Tag = "My Plans";

    private readonly ISender _sender;

    public MyPlansController(ISender sender)
    {
        _sender = sender;
    }

    // GET /api/my/meal-plans
    [HttpGet("meal-plans")]
    [ProducesResponseType(typeof(List<MealPlanListItemDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "Get my meal plans",
        Description = "All meal plans assigned to the authenticated client, most recent first.",
        Tags = new[] { Tag })]
    public async Task<ActionResult<List<MealPlanListItemDto>>> GetMyMealPlans(
        CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetMyMealPlansQuery(), cancellationToken));

    // GET /api/my/meal-plans/{planId}
    [HttpGet("meal-plans/{planId:guid}")]
    [ProducesResponseType(typeof(MealPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Get one of my meal plans",
        Description = "Full detail (entries + macro totals) of a meal plan that belongs to the " +
                      "authenticated client. 404 if it isn't theirs.",
        Tags = new[] { Tag })]
    public async Task<ActionResult<MealPlanDto>> GetMyMealPlan(
        Guid planId,
        CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetMyMealPlanByIdQuery(planId), cancellationToken));

    // GET /api/my/training-plans
    [HttpGet("training-plans")]
    [ProducesResponseType(typeof(List<TrainingPlanListItemDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "Get my training plans",
        Description = "All weekly training plans assigned to the authenticated client, most recent first.",
        Tags = new[] { Tag })]
    public async Task<ActionResult<List<TrainingPlanListItemDto>>> GetMyTrainingPlans(
        CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetMyTrainingPlansQuery(), cancellationToken));

    // GET /api/my/training-plans/{planId}
    [HttpGet("training-plans/{planId:guid}")]
    [ProducesResponseType(typeof(WeeklyTrainingPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Get one of my training plans",
        Description = "Full detail (days + routine names) of a training plan that belongs to the " +
                      "authenticated client. 404 if it isn't theirs.",
        Tags = new[] { Tag })]
    public async Task<ActionResult<WeeklyTrainingPlanDto>> GetMyTrainingPlan(
        Guid planId,
        CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetMyTrainingPlanByIdQuery(planId), cancellationToken));
}
