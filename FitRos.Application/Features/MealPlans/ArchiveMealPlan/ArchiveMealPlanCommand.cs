using MediatR;

namespace FitRos.Application.Features.MealPlans.ArchiveMealPlan;

public record ArchiveMealPlanCommand(Guid MealPlanId) : IRequest;
