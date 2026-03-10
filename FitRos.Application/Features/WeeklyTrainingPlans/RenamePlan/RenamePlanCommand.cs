using MediatR;

namespace FitRos.Application.Features.WeeklyTrainingPlans.RenamePlan;

public record RenamePlanCommand(Guid PlanId, string Name) : IRequest;
