namespace FitRos.Domain.Entities.Training;

public class WorkoutRoutineExercise
{
    public Guid Id { get; private set; }

    public Guid ExerciseId { get; private set; }

    public int Order { get; private set; }

    public int SuggestedSets { get; private set; }

    public int SuggestedReps { get; private set; }

    public int SuggestedRestSeconds { get; private set; }

    private WorkoutRoutineExercise() { }

    internal WorkoutRoutineExercise(
        Guid exerciseId,
        int order,
        int suggestedSets,
        int suggestedReps,
        int suggestedRestSeconds)
    {
        Id = Guid.NewGuid();
        ExerciseId = exerciseId;
        Order = order;
        SuggestedSets = suggestedSets;
        SuggestedReps = suggestedReps;
        SuggestedRestSeconds = suggestedRestSeconds;
    }
}
