namespace FitRos.Application.Features.WorkoutRoutines.MoveExerciseInWorkoutRoutine;

public record MoveExerciseInWorkoutRoutineCommand(
    Guid WorkoutRoutineId,
    Guid ExerciseId,
    int NewOrder
);
