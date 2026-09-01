using MediatR;

namespace FitRos.Application.Features.WeeklyTrainingPlans.ArchivePlan;

public record ArchivePlanCommand(Guid PlanId) : IRequest;
