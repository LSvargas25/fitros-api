using MediatR;

namespace FitRos.Application.Features.MealPlans.ActivateMealPlan;

public record ActivateMealPlanCommand(Guid MealPlanId) : IRequest;
