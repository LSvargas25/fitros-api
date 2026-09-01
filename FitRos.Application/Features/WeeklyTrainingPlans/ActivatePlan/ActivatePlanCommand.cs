using MediatR;

namespace FitRos.Application.Features.WeeklyTrainingPlans.ActivatePlan;

public record ActivatePlanCommand(Guid PlanId) : IRequest;
