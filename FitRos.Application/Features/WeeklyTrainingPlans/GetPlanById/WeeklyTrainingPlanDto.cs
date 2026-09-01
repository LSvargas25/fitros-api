using FitRos.Domain.Entities.Enums;

namespace FitRos.Application.Features.WeeklyTrainingPlans.GetPlanById;

public record WeeklyTrainingPlanDto(
    Guid Id,
    Guid ClientProfileId,
    Guid? CoachId,
    string Name,
    TrainingPlanStatus Status,
    DateTime CreatedAt,
    List<TrainingPlanDayDto> Days
);
