using MediatR;

namespace FitRos.Application.Features.WeeklyTrainingPlans.GetClientPlans;

public record GetClientPlansQuery(Guid ClientProfileId) : IRequest<List<TrainingPlanListItemDto>>;
