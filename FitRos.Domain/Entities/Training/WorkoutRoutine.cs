using FitRos.Domain.Common;
using FitRos.Domain.Enums;

namespace FitRos.Domain.Entities.Training;

public class WorkoutRoutine
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;
    public string NormalizedName { get; private set; } = null!;
    public string Description { get; private set; } = null!;

    public int Version { get; private set; }

    public RoutineStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private readonly List<WorkoutRoutineExercise> _exercises = new();

    public IReadOnlyCollection<WorkoutRoutineExercise> Exercises => _exercises;

    private WorkoutRoutine() { } // EF

    private WorkoutRoutine(Guid id, string name, string description)
    {
        Id = id;
        Name = name;
        NormalizedName = name.ToLowerInvariant();
        Description = description;
        Version = 1;
        Status = RoutineStatus.Draft;
        CreatedAt = DateTime.UtcNow;
    }

    // Create routine
    public static WorkoutRoutine Create(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Routine name cannot be empty.");

        return new WorkoutRoutine(
            Guid.NewGuid(),
            name.Trim(),
            description.Trim());
    }

    // Add exercise
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

    // Remove exercise
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

    // Reorder exercises after removal
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
    // Move exercise to new order
    public void MoveExercise(Guid exerciseId, int newOrder)
    {
        EnsureDraftState();

        var exercise = _exercises
            .FirstOrDefault(e => e.ExerciseId == exerciseId);

        if (exercise is null)
            throw new DomainException("Exercise not found in routine.");

        if (newOrder <= 0 || newOrder > _exercises.Count)
            throw new DomainException("Invalid new order.");

        var currentOrder = exercise.Order;

        if (currentOrder == newOrder)
            return;

        if (currentOrder < newOrder)
        {
            foreach (var e in _exercises)
            {
                if (e.Order > currentOrder && e.Order <= newOrder)
                    e.SetOrder(e.Order - 1);
            }
        }
        else
        {
            foreach (var e in _exercises)
            {
                if (e.Order >= newOrder && e.Order < currentOrder)
                    e.SetOrder(e.Order + 1);
            }
        }

        exercise.SetOrder(newOrder);
    }



    // Ensure routine is editable
    private void EnsureDraftState()
    {
        if (Status != RoutineStatus.Draft)
            throw new InvalidOperationException("Routine can only be modified in Draft state.");
    }

    // Publish routine
    public void Publish()
    {
        if (Status != RoutineStatus.Draft)
            throw new InvalidOperationException("Only draft routines can be published.");

        if (!_exercises.Any())
            throw new InvalidOperationException("Cannot publish a routine without exercises.");

        Status = RoutineStatus.Published;
        Version++;
    }

    // Archive routine
    public void Archive()
    {
        if (Status != RoutineStatus.Published)
            throw new DomainException("Only published routines can be archived.");

        Status = RoutineStatus.Archived;
        Version++;
    }

    // Create new version
    public WorkoutRoutine CreateNewVersion()
    {
        if (Status != RoutineStatus.Published)
            throw new InvalidOperationException("Only published routines can be versioned.");

        var newRoutine = new WorkoutRoutine(
            Guid.NewGuid(),
            Name,
            Description);

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

    // Update details
    public void UpdateDetails(string name, string description)
    {
        EnsureDraftState();

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Routine name cannot be empty.");

        Name = name.Trim();
        NormalizedName = Name.ToLowerInvariant();
        Description = description.Trim();

        Version++;
    }

    internal void BreakOrderForMove(Guid exerciseId)
    {
        var exercise = _exercises
            .FirstOrDefault(e => e.ExerciseId == exerciseId);

        if (exercise is null)
            throw new DomainException("Exercise not found in routine.");

        exercise.SetOrder(-1000);
    }

}
