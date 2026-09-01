namespace FitRos.Domain.Entities.Training;

public class ExerciseSet
{
    public Guid Id { get; private set; }

    public Guid ExerciseId { get; private set; }

    public int SetNumber { get; private set; }

    public int RepsAchieved { get; private set; }

    public decimal WeightUsed { get; private set; }

    private ExerciseSet() { }

    internal ExerciseSet(
        Guid exerciseId,
        int setNumber,
        int repsAchieved,
        decimal weightUsed)
    {
        Id = Guid.NewGuid();
        ExerciseId = exerciseId;
        SetNumber = setNumber;
        RepsAchieved = repsAchieved;
        WeightUsed = weightUsed;
    }

    internal void Update(int setNumber, int repsAchieved, decimal weightUsed)
    {
        SetNumber = setNumber;
        RepsAchieved = repsAchieved;
        WeightUsed = weightUsed;
    }
}
