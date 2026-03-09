using MediatR;

namespace FitRos.Application.Features.WeeklyTrainingPlans.AssignRoutineToDay;

public record AssignRoutineToDayCommand(
    Guid PlanId,
    DayOfWeek Day,
    Guid WorkoutRoutineId,
    string? Notes) : IRequest;
