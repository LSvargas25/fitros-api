using FitRos.Domain.Enums;

namespace FitRos.Domain.Entities.Training;

public class WorkoutRoutine
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string Description { get; private set; } = null!;

    public int Version { get; private set; }

    public RoutineStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private readonly List<WorkoutRoutineExercise> _exercises = new();
    public IReadOnlyCollection<WorkoutRoutineExercise> Exercises => _exercises.AsReadOnly();

    // Constructor for EF
    private WorkoutRoutine() { }

    //model workoutRoutine
    private WorkoutRoutine(
        Guid id,
        string name,
        string description,
        int version)
    {
        Id = id;
        Name = name;
        Description = description;
        Version = version;
        Status = RoutineStatus.Draft;
        CreatedAt = DateTime.UtcNow;
    }

    //Create Routine
    public static WorkoutRoutine Create(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Routine name cannot be empty.");

        return new WorkoutRoutine(
            Guid.NewGuid(),
            name.Trim(),
            description.Trim(),
            version: 1);
    }
    //Add Exercise
    public void AddExercise(
        Guid exerciseId,
        int order,
        int suggestedSets,
        int suggestedReps,
        int suggestedRestSeconds)
    {
        EnsureDraftState();

        _exercises.Add(new WorkoutRoutineExercise(
            exerciseId,
            order,
            suggestedSets,
            suggestedReps,
            suggestedRestSeconds));
    }

    //State of Routine publish
    public void Publish()
    {
        if (!_exercises.Any())
            throw new InvalidOperationException("Cannot publish a routine without exercises.");

        Status = RoutineStatus.Published;
    }

    public void Archive()
    {
        Status = RoutineStatus.Archived;
    }

    //Metod create a new Version
    public WorkoutRoutine CreateNewVersion()
    {
        if (Status != RoutineStatus.Published)
            throw new InvalidOperationException("Only published routines can be versioned.");

        var newRoutine = new WorkoutRoutine(
            Guid.NewGuid(),
            Name,
            Description,
            Version + 1);

        foreach (var exercise in _exercises)
        {
            newRoutine._exercises.Add(
                new WorkoutRoutineExercise(
                    exercise.ExerciseId,
                    exercise.Order,
                    exercise.SuggestedSets,
                    exercise.SuggestedReps,
                    exercise.SuggestedRestSeconds));
        }

        return newRoutine;
    }

    //Ensure State to update
    private void EnsureDraftState()
    {
        if (Status != RoutineStatus.Draft)
            throw new InvalidOperationException("Routine can only be modified in Draft state.");
    }

    //Metod update 
    public void UpdateDetails(string name, string description)
    {
        EnsureDraftState();

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Routine name cannot be empty.");

        Name = name.Trim();
        Description = description.Trim();
    }



}
