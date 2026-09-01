using MediatR;

namespace FitRos.Application.Features.WeeklyTrainingPlans.GetPlanById;

public record GetWeeklyTrainingPlanByIdQuery(Guid PlanId) : IRequest<WeeklyTrainingPlanDto>;
