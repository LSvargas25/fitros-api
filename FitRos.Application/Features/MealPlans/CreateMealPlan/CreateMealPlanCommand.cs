using MediatR;

namespace FitRos.Application.Features.MealPlans.CreateMealPlan;

public record CreateMealPlanCommand(
    Guid ClientProfileId,
    string Name
) : IRequest<CreateMealPlanResponse>;
