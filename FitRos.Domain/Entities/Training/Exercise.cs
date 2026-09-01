using FitRos.Domain.Common;
using FitRos.Domain.Entities.Enums;

namespace FitRos.Domain.Entities.Training;

public class Exercise : ITenantEntity
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string NormalizedName { get; private set; } = null!;

    public string Description { get; private set; } = null!;

    public MuscleGroup Category { get; private set; }

    public bool IsArchived { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public Guid? GymId { get; private set; }

    Guid? ITenantEntity.GymId
    {
        get => GymId;
        set => GymId = value;
    }

    private Exercise() { }

    private Exercise(
        Guid id,
        string name,
        string description,
        MuscleGroup category,
        Guid? gymId)
    {
        Id = id;
        Name = name;
        NormalizedName = name.ToLowerInvariant();
        Description = description;
        Category = category;
        IsArchived = false;
        CreatedAt = DateTime.UtcNow;
        GymId = gymId;
    }

    public static Exercise Create(
     string name,
     string description,
     MuscleGroup category,
     Guid? gymId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Exercise name cannot be empty.");

        name = name.Trim();

        return new Exercise(
            Guid.NewGuid(),
            name,
            description.Trim(),
            category,
            gymId);
    }

    public void Update(
        string name,
        string description,
        MuscleGroup category)
    {
        EnsureNotArchived();

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Exercise name cannot be empty.");

        name = name.Trim();

        Name = name;
        NormalizedName = name.ToLowerInvariant();
        Description = description.Trim();
        Category = category;
    }
    
    public void Archive()
    {
        if (IsArchived)
            throw new InvalidOperationException("Exercise is already archived.");

        IsArchived = true;
    }

    public void Restore()
    {
        if (!IsArchived)
            throw new InvalidOperationException("Exercise is not archived.");

        IsArchived = false;
    }

    private void EnsureNotArchived()
    {
        if (IsArchived)
            throw new InvalidOperationException("Archived exercises cannot be modified.");
    }
}