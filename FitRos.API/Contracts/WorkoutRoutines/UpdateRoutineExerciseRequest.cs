namespace FitRos.API.Contracts.WorkoutRoutines;

public record UpdateRoutineExerciseRequest(
    int SuggestedSets,
    int SuggestedReps,
    int SuggestedRestSeconds);
