namespace FitRos.Application.Features.WorkoutRoutines.CreateWorkoutRoutineVersion;

public sealed record CreateWorkoutRoutineVersionCommand(
    Guid WorkoutRoutineId
);