using MediatR;

namespace FitRos.Application.Features.WeeklyTrainingPlans.CreatePlan;

public record CreateWeeklyTrainingPlanCommand(
    Guid ClientProfileId,
    string Name
) : IRequest<CreateWeeklyTrainingPlanResponse>;
