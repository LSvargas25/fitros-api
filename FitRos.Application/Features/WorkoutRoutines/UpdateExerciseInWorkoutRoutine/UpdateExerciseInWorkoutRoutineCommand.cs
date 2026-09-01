namespace FitRos.Application.Features.WorkoutRoutines.UpdateExerciseInWorkoutRoutine;

public record UpdateExerciseInWorkoutRoutineCommand(
    Guid WorkoutRoutineId,
    Guid ExerciseId,
    int SuggestedSets,
    int SuggestedReps,
    int SuggestedRestSeconds
);
