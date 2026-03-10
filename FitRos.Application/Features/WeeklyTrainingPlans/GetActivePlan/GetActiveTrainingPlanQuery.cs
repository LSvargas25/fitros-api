using FitRos.Application.Features.WeeklyTrainingPlans.GetPlanById;
using MediatR;

namespace FitRos.Application.Features.WeeklyTrainingPlans.GetActivePlan;

public record GetActiveTrainingPlanQuery(Guid ClientProfileId) : IRequest<WeeklyTrainingPlanDto?>;
