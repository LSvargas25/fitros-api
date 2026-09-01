namespace FitRos.Domain.Entities.WeeklyTraining;

public sealed class TrainingPlanDay
{
    public Guid Id { get; private set; }

    public Guid WeeklyTrainingPlanId { get; private set; }

    public DayOfWeek Day { get; private set; }

    public Guid WorkoutRoutineId { get; private set; }

    public string? Notes { get; private set; }

    private TrainingPlanDay() { }

    internal static TrainingPlanDay Create(
        Guid planId,
        DayOfWeek day,
        Guid routineId,
        string? notes)
    {
        return new TrainingPlanDay
        {
            Id = Guid.NewGuid(),
            WeeklyTrainingPlanId = planId,
            Day = day,
            WorkoutRoutineId = routineId,
            Notes = notes?.Trim()
        };
    }

    internal void UpdateRoutine(Guid routineId, string? notes)
    {
        WorkoutRoutineId = routineId;
        Notes = notes?.Trim();
    }
}
