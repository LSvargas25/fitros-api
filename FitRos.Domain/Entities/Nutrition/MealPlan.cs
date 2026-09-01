using FitRos.Domain.Common;
using FitRos.Domain.Entities.Enums;

namespace FitRos.Domain.Entities.Nutrition;

/// <summary>
/// A meal plan owned by a coach and assigned to a single client. Mirrors the
/// <c>WeeklyTrainingPlan</c> aggregate: a Draft/Active/Archived lifecycle plus a
/// collection of per-day / per-meal food entries.
/// </summary>
public sealed class MealPlan : AggregateRoot, ITenantEntity
{
    private readonly List<MealPlanEntry> _entries = new();

    public Guid Id { get; private set; }

    public Guid ClientProfileId { get; private set; }

    public Guid CoachId { get; private set; }

    public string Name { get; private set; } = null!;

    public MealPlanStatus Status { get; private set; }

    public Guid? GymId { get; private set; }

    Guid? ITenantEntity.GymId
    {
        get => GymId;
        set => GymId = value;
    }

    public IReadOnlyCollection<MealPlanEntry> Entries => _entries.AsReadOnly();

    private MealPlan() { }

    public static MealPlan Create(
        Guid clientProfileId,
        Guid coachId,
        Guid? gymId,
        string name)
    {
        if (clientProfileId == Guid.Empty)
            throw new DomainException("ClientProfileId cannot be empty.");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Meal plan name cannot be empty.");

        return new MealPlan
        {
            Id = Guid.NewGuid(),
            ClientProfileId = clientProfileId,
            CoachId = coachId,
            GymId = gymId,
            Name = name.Trim(),
            Status = MealPlanStatus.Draft,
            CreatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Adds a food to a (day, meal) slot. If the same food is already in that
    /// slot the quantity is replaced rather than duplicated.
    /// </summary>
    public void AddEntry(DayOfWeek day, MealType meal, Guid foodId, decimal quantityGrams)
    {
        EnsureNotArchived();

        if (foodId == Guid.Empty)
            throw new DomainException("FoodId cannot be empty.");

        var existing = _entries.FirstOrDefault(
            e => e.Day == day && e.Meal == meal && e.FoodId == foodId);

        if (existing is not null)
        {
            existing.UpdateQuantity(quantityGrams);
            return;
        }

        _entries.Add(MealPlanEntry.Create(Id, day, meal, foodId, quantityGrams));
    }

    public void UpdateEntry(Guid entryId, decimal quantityGrams)
    {
        EnsureNotArchived();

        var entry = _entries.FirstOrDefault(e => e.Id == entryId)
            ?? throw new NotFoundException("Meal plan entry not found.");

        entry.UpdateQuantity(quantityGrams);
    }

    public void RemoveEntry(Guid entryId)
    {
        EnsureNotArchived();

        var entry = _entries.FirstOrDefault(e => e.Id == entryId)
            ?? throw new NotFoundException("Meal plan entry not found.");

        _entries.Remove(entry);
    }

    public void Rename(string name)
    {
        EnsureNotArchived();

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Meal plan name cannot be empty.");

        Name = name.Trim();
    }

    public void Activate()
    {
        if (Status == MealPlanStatus.Active)
            throw new DomainException("Meal plan is already active.");

        if (Status == MealPlanStatus.Archived)
            throw new DomainException("Archived meal plans cannot be activated.");

        Status = MealPlanStatus.Active;
    }

    public void Deactivate()
    {
        if (Status != MealPlanStatus.Active)
            throw new DomainException("Only active meal plans can be deactivated.");

        Status = MealPlanStatus.Draft;
    }

    public void Archive()
    {
        if (Status == MealPlanStatus.Archived)
            throw new DomainException("Meal plan is already archived.");

        Status = MealPlanStatus.Archived;
    }

    private void EnsureNotArchived()
    {
        if (Status == MealPlanStatus.Archived)
            throw new DomainException("Cannot modify an archived meal plan.");
    }
}
