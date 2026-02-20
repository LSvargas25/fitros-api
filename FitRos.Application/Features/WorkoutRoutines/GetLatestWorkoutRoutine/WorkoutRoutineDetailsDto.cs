namespace FitRos.Application.Features.WorkoutRoutines;

public sealed record WorkoutRoutineDetailsDto(
    Guid Id,
    string Name,
    string Description,
    int Version,
    FitRos.Domain.Enums.RoutineStatus Status,
    List<WorkoutRoutineExerciseDto> Exercises
);