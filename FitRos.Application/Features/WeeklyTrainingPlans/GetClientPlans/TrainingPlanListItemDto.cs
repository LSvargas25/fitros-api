using FitRos.Domain.Entities.Enums;

namespace FitRos.Application.Features.WeeklyTrainingPlans.GetClientPlans;

public record TrainingPlanListItemDto(
    Guid Id,
    string Name,
    TrainingPlanStatus Status,
    int DayCount,
    DateTime CreatedAt
);
