using FitRos.Application.Features.MealPlans.GetMealPlanById;
using MediatR;

namespace FitRos.Application.Features.MealPlans.GetActiveMealPlan;

public record GetActiveMealPlanQuery(Guid ClientProfileId) : IRequest<MealPlanDto?>;
