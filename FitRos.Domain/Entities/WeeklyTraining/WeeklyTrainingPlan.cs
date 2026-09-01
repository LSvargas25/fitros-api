using FitRos.Domain.Common;
using FitRos.Domain.Entities.Enums;

namespace FitRos.Domain.Entities.WeeklyTraining;

public sealed class WeeklyTrainingPlan : AggregateRoot, ITenantEntity
{
    private readonly List<TrainingPlanDay> _days = new();

    public Guid Id { get; private set; }

    public Guid ClientProfileId { get; private set; }

    public Guid? CoachId { get; private set; }

    public string Name { get; private set; } = null!;

    public TrainingPlanStatus Status { get; private set; }

    public Guid? GymId { get; private set; }

    Guid? ITenantEntity.GymId
    {
        get => GymId;
        set => GymId = value;
    }

    public IReadOnlyCollection<TrainingPlanDay> Days => _days.AsReadOnly();

    private WeeklyTrainingPlan() { }

    public static WeeklyTrainingPlan Create(
        Guid clientProfileId,
        Guid? coachId,
        Guid? gymId,
        string name)
    {
        if (clientProfileId == Guid.Empty)
            throw new DomainException("ClientProfileId cannot be empty.");

        if (coachId == Guid.Empty)
            throw new DomainException("CoachId cannot be empty.");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Training plan name cannot be empty.");

        return new WeeklyTrainingPlan
        {
            Id = Guid.NewGuid(),
            ClientProfileId = clientProfileId,
            CoachId = coachId,
            GymId = gymId,
            Name = name.Trim(),
            Status = TrainingPlanStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void AssignRoutineToDay(DayOfWeek day, Guid routineId, string? notes = null)
    {
        EnsureNotArchived();

        if (routineId == Guid.Empty)
            throw new DomainException("WorkoutRoutineId cannot be empty.");

        var existing = _days.FirstOrDefault(d => d.Day == day);

        if (existing is not null)
        {
            existing.UpdateRoutine(routineId, notes);
            return;
        }

        _days.Add(TrainingPlanDay.Create(Id, day, routineId, notes));
    }

    public void RemoveRoutineFromDay(DayOfWeek day)
    {
        EnsureNotArchived();

        var entry = _days.FirstOrDefault(d => d.Day == day);

        if (entry is null)
            throw new DomainException("No routine is assigned to this day.");

        _days.Remove(entry);
    }

    public void Activate()
    {
        if (Status == TrainingPlanStatus.Active)
            throw new DomainException("Training plan is already active.");

        if (Status == TrainingPlanStatus.Archived)
            throw new DomainException("Archived training plans cannot be activated.");

        Status = TrainingPlanStatus.Active;
    }

    public void Deactivate()
    {
        if (Status != TrainingPlanStatus.Active)
            throw new DomainException("Only active training plans can be deactivated.");

        Status = TrainingPlanStatus.Draft;
    }

    public void Archive()
    {
        if (Status == TrainingPlanStatus.Archived)
            throw new DomainException("Training plan is already archived.");

        Status = TrainingPlanStatus.Archived;
    }

    public void Rename(string name)
    {
        EnsureNotArchived();

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Training plan name cannot be empty.");

        Name = name.Trim();
    }

    private void EnsureNotArchived()
    {
        if (Status == TrainingPlanStatus.Archived)
            throw new DomainException("Cannot modify an archived training plan.");
    }
}
