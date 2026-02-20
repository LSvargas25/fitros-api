namespace FitRos.Application.Features.WorkoutRoutines.RemoveExerciseFromWorkoutRoutine;

public record RemoveExerciseFromWorkoutRoutineCommand(
    Guid WorkoutRoutineId,
    Guid ExerciseId
);
