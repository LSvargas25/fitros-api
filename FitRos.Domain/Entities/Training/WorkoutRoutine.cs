using FitRos.Domain.Common;
using FitRos.Domain.Enums;

namespace FitRos.Domain.Entities.Training;

public class WorkoutRoutine
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;
    public string NormalizedName { get; private set; } = null!;
    public string Description { get; private set; } = null!;

    // Logical version within the routine group (v1, v2, v3...)
    public int Version { get; private set; }

    // Identifier that ties all versions together
    public Guid RoutineGroupId { get; private set; }

    public RoutineStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private readonly List<WorkoutRoutineExercise> _exercises = new();
    public IReadOnlyCollection<WorkoutRoutineExercise> Exercises => _exercises;

    private WorkoutRoutine() { } // EF Core

    private WorkoutRoutine(Guid id, string name, string description)
    {
        Id = id;

        // First version uses its own Id as the group identifier
        RoutineGroupId = id;

        Name = name;
        NormalizedName = name.ToLowerInvariant();
        Description = description;

        Version = 1;
        Status = RoutineStatus.Draft;
        CreatedAt = DateTime.UtcNow;
    }

    // ============================
    // Factory
    // ============================

    public static WorkoutRoutine Create(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Routine name cannot be empty.");

        return new WorkoutRoutine(
            Guid.NewGuid(),
            name.Trim(),
            description.Trim());
    }

    // ============================
    // Exercises
    // ============================

    public void AddExercise(
        Guid exerciseId,
        int order,
        int suggestedSets,
        int suggestedReps,
        int suggestedRestSeconds)
    {
        EnsureDraftState();

        if (exerciseId == Guid.Empty)
            throw new DomainException("ExerciseId cannot be empty.");

        if (order <= 0)
            throw new DomainException("Order must be greater than zero.");

        if (suggestedSets <= 0)
            throw new DomainException("Suggested sets must be greater than zero.");

        if (suggestedReps <= 0)
            throw new DomainException("Suggested reps must be greater than zero.");

        if (suggestedRestSeconds < 0)
            throw new DomainException("Suggested rest seconds cannot be negative.");

        if (_exercises.Any(e => e.ExerciseId == exerciseId))
            throw new DomainException("This exercise is already part of the routine.");

        if (_exercises.Any(e => e.Order == order))
            throw new DomainException("This order position is already used in the routine.");

        _exercises.Add(new WorkoutRoutineExercise(
            Id,
            exerciseId,
            order,
            suggestedSets,
            suggestedReps,
            suggestedRestSeconds));
    }

    public void RemoveExercise(Guid exerciseId)
    {
        EnsureDraftState();

        var exercise = _exercises
            .FirstOrDefault(e => e.ExerciseId == exerciseId);

        if (exercise is null)
            throw new DomainException("Exercise not found in routine.");

        _exercises.Remove(exercise);

        ReorderExercises();
    }

    public void MoveExercise(Guid exerciseId, int newOrder, int originalOrder)
    {
        EnsureDraftState();

        var exercise = _exercises
            .FirstOrDefault(e => e.ExerciseId == exerciseId);

        if (exercise is null)
            throw new DomainException("Exercise not found in routine.");

        if (newOrder <= 0)
            throw new DomainException("New order must be greater than zero.");

        if (newOrder > _exercises.Count)
            throw new DomainException("New order exceeds the number of exercises.");

        if (originalOrder == newOrder)
            return;

        if (originalOrder < newOrder)
        {
            foreach (var e in _exercises)
            {
                if (e.Order > originalOrder && e.Order <= newOrder)
                    e.SetOrder(e.Order - 1);
            }
        }
        else
        {
            foreach (var e in _exercises)
            {
                if (e.Order >= newOrder && e.Order < originalOrder)
                    e.SetOrder(e.Order + 1);
            }
        }

        exercise.SetOrder(newOrder);
    }

    private void ReorderExercises()
    {
        var ordered = _exercises
            .OrderBy(e => e.Order)
            .ToList();

        for (int i = 0; i < ordered.Count; i++)
        {
            ordered[i].SetOrder(i + 1);
        }
    }

    // ============================
    // State Transitions
    // ============================

    private void EnsureDraftState()
    {
        if (Status != RoutineStatus.Draft)
            throw new DomainException("Routine can only be modified in Draft state.");
    }

    public void Publish()
    {
        if (Status != RoutineStatus.Draft)
            throw new DomainException("Only draft routines can be published.");

        if (!_exercises.Any())
            throw new DomainException("Cannot publish a routine without exercises.");

        Status = RoutineStatus.Published;

        // IMPORTANT:
        // Publishing does NOT change logical version.
    }

    public void Archive()
    {
        if (Status != RoutineStatus.Published)
            throw new DomainException("Only published routines can be archived.");

        Status = RoutineStatus.Archived;

        // IMPORTANT:
        // Archiving does NOT change logical version.
    }

    // ============================
    // Versioning
    // ============================

    public WorkoutRoutine CreateNewVersion()
    {
        if (Status != RoutineStatus.Published)
            throw new DomainException("Only published routines can be versioned.");

        var newRoutine = new WorkoutRoutine(
            Guid.NewGuid(),
            Name,
            Description);

        // Keep same group identifier
        newRoutine.RoutineGroupId = this.RoutineGroupId;

        // Logical version increment
        newRoutine.Version = this.Version + 1;

        foreach (var exercise in _exercises)
        {
            newRoutine._exercises.Add(
                new WorkoutRoutineExercise(
                    newRoutine.Id,
                    exercise.ExerciseId,
                    exercise.Order,
                    exercise.SuggestedSets,
                    exercise.SuggestedReps,
                    exercise.SuggestedRestSeconds));
        }

        return newRoutine;
    }

    // ============================
    // Updates
    // ============================

    public void UpdateDetails(string name, string description)
    {
        EnsureDraftState();

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Routine name cannot be empty.");

        Name = name.Trim();
        NormalizedName = Name.ToLowerInvariant();
        Description = description.Trim();

        // IMPORTANT:
        // Updating details does NOT change logical version.
    }
}