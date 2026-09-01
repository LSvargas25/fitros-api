using MediatR;

namespace FitRos.Application.Features.MealPlans.GetMealPlanById;

public record GetMealPlanByIdQuery(Guid MealPlanId) : IRequest<MealPlanDto>;
