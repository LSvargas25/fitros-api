namespace FitRos.Application.Features.WorkoutRoutines;

public sealed record WorkoutRoutineExerciseDto(
    Guid ExerciseId,
    int Order,
    int SuggestedSets,
    int SuggestedReps,
    int SuggestedRestSeconds
);