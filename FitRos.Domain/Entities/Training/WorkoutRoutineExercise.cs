namespace FitRos.Domain.Entities.Training;

public class WorkoutRoutineExercise
{
    public Guid Id { get; private set; }

    public Guid WorkoutRoutineId { get; private set; }

    public Guid ExerciseId { get; private set; }

    public int Order { get; private set; }

    public int SuggestedSets { get; private set; }

    public int SuggestedReps { get; private set; }

    public int SuggestedRestSeconds { get; private set; }

    public WorkoutRoutine WorkoutRoutine { get; private set; } = null!;

    private WorkoutRoutineExercise() { } // EF

    internal WorkoutRoutineExercise(
        Guid workoutRoutineId,
        Guid exerciseId,
        int order,
        int suggestedSets,
        int suggestedReps,
        int suggestedRestSeconds)
    {
        if (workoutRoutineId == Guid.Empty)
            throw new ArgumentException("WorkoutRoutineId cannot be empty.");

        if (exerciseId == Guid.Empty)
            throw new ArgumentException("ExerciseId cannot be empty.");

        if (order <= 0)
            throw new ArgumentException("Order must be greater than zero.");

        if (suggestedSets <= 0)
            throw new ArgumentException("Suggested sets must be greater than zero.");

        if (suggestedReps <= 0)
            throw new ArgumentException("Suggested reps must be greater than zero.");

        if (suggestedRestSeconds < 0)
            throw new ArgumentException("Suggested rest seconds cannot be negative.");

        Id = Guid.NewGuid();
        WorkoutRoutineId = workoutRoutineId;
        ExerciseId = exerciseId;
        Order = order;
        SuggestedSets = suggestedSets;
        SuggestedReps = suggestedReps;
        SuggestedRestSeconds = suggestedRestSeconds;
    }

    // Internal because only the Aggregate Root should reorder
    internal void SetOrder(int newOrder)
    {
        Order = newOrder;
    }
}
