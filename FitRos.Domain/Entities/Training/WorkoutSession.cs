using FitRos.Domain.Enums;

namespace FitRos.Domain.Entities.Training;

public class WorkoutSession
{
    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid RoutineId { get; private set; }

    public string RoutineNameSnapshot { get; private set; } = null!;

    public int RoutineVersion { get; private set; }

    public DateTime ScheduledDate { get; private set; }

    public WorkoutSessionStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private readonly List<ExerciseSet> _sets = new();
    public IReadOnlyCollection<ExerciseSet> Sets => _sets.AsReadOnly();

    private WorkoutSession() { }

    private WorkoutSession(
        Guid userId,
        Guid routineId,
        string routineName,
        int routineVersion,
        DateTime scheduledDate)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        RoutineId = routineId;
        RoutineNameSnapshot = routineName;
        RoutineVersion = routineVersion;
        ScheduledDate = scheduledDate;
        Status = WorkoutSessionStatus.Scheduled;
        CreatedAt = DateTime.UtcNow;
    }

    public static WorkoutSession Create(
        Guid userId,
        Guid routineId,
        string routineName,
        int routineVersion,
        DateTime scheduledDate)
    {
        return new WorkoutSession(
            userId,
            routineId,
            routineName,
            routineVersion,
            scheduledDate);
    }

    public void Start()
    {
        if (Status != WorkoutSessionStatus.Scheduled)
            throw new InvalidOperationException("Session must be scheduled to start.");

        Status = WorkoutSessionStatus.InProgress;
    }

    public void AddSet(
        Guid exerciseId,
        int setNumber,
        int repsAchieved,
        decimal weightUsed)
    {
        if (Status != WorkoutSessionStatus.InProgress)
            throw new InvalidOperationException("Cannot add sets unless session is in progress.");

        _sets.Add(new ExerciseSet(
            exerciseId,
            setNumber,
            repsAchieved,
            weightUsed));
    }

    public void Complete()
    {
        if (Status != WorkoutSessionStatus.InProgress)
            throw new InvalidOperationException("Only in-progress sessions can be completed.");

        if (!_sets.Any())
            throw new InvalidOperationException("Cannot complete a session without sets.");

        Status = WorkoutSessionStatus.Completed;
    }


    public void Skip()
    {
        if (Status == WorkoutSessionStatus.Completed)
            throw new InvalidOperationException("Cannot skip a completed session.");

        Status = WorkoutSessionStatus.Skipped;
    }
}
