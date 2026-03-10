namespace FitRos.Application.Features.WeeklyTrainingPlans.GetPlanById;

public record TrainingPlanDayDto(
    Guid Id,
    DayOfWeek Day,
    Guid WorkoutRoutineId,
    string WorkoutRoutineName,
    string? Notes
);
