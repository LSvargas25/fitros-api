using MediatR;

namespace FitRos.Application.Features.MealPlans.RenameMealPlan;

public record RenameMealPlanCommand(Guid MealPlanId, string Name) : IRequest;
