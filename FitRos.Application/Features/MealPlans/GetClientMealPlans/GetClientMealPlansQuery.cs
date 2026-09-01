using MediatR;

namespace FitRos.Application.Features.MealPlans.GetClientMealPlans;

public record GetClientMealPlansQuery(Guid ClientProfileId)
    : IRequest<List<MealPlanListItemDto>>;
