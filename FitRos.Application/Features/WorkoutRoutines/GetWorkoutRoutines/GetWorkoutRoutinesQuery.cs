using FitRos.Domain.Enums;

namespace FitRos.Application.Features.WorkoutRoutines.GetWorkoutRoutines;

public record GetWorkoutRoutinesQuery(
    RoutineStatus? Status,
    int Page = 1,
    int PageSize = 10
);
