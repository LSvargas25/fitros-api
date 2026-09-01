using FitRos.Domain.Common;
using FitRos.Domain.Entities.Enums;

namespace FitRos.Domain.Entities.Nutrition;

/// <summary>
/// A food item in the gym's catalog. Macros are stored per 100 g (the
/// canonical unit for computing meal totals from a gram quantity); an optional
/// <see cref="ServingSizeGrams"/> records a typical portion for UI convenience.
/// </summary>
public class Food : ITenantEntity
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string NormalizedName { get; private set; } = null!;

    public FoodCategory Category { get; private set; }

    public decimal CaloriesPer100g { get; private set; }

    public decimal ProteinPer100g { get; private set; }

    public decimal CarbsPer100g { get; private set; }

    public decimal FatPer100g { get; private set; }

    public decimal? ServingSizeGrams { get; private set; }

    public bool IsArchived { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public Guid? GymId { get; private set; }

    Guid? ITenantEntity.GymId
    {
        get => GymId;
        set => GymId = value;
    }

    private Food() { }

    private Food(
        Guid id,
        string name,
        FoodCategory category,
        decimal caloriesPer100g,
        decimal proteinPer100g,
        decimal carbsPer100g,
        decimal fatPer100g,
        decimal? servingSizeGrams,
        Guid? gymId)
    {
        Id = id;
        Name = name;
        NormalizedName = name.ToLowerInvariant();
        Category = category;
        CaloriesPer100g = caloriesPer100g;
        ProteinPer100g = proteinPer100g;
        CarbsPer100g = carbsPer100g;
        FatPer100g = fatPer100g;
        ServingSizeGrams = servingSizeGrams;
        IsArchived = false;
        CreatedAt = DateTime.UtcNow;
        GymId = gymId;
    }

    public static Food Create(
        string name,
        FoodCategory category,
        decimal caloriesPer100g,
        decimal proteinPer100g,
        decimal carbsPer100g,
        decimal fatPer100g,
        decimal? servingSizeGrams,
        Guid? gymId)
    {
        ValidateName(name);
        ValidateMacros(caloriesPer100g, proteinPer100g, carbsPer100g, fatPer100g);
        ValidateServingSize(servingSizeGrams);

        return new Food(
            Guid.NewGuid(),
            name.Trim(),
            category,
            caloriesPer100g,
            proteinPer100g,
            carbsPer100g,
            fatPer100g,
            servingSizeGrams,
            gymId);
    }

    public void Update(
        string name,
        FoodCategory category,
        decimal caloriesPer100g,
        decimal proteinPer100g,
        decimal carbsPer100g,
        decimal fatPer100g,
        decimal? servingSizeGrams)
    {
        EnsureNotArchived();

        ValidateName(name);
        ValidateMacros(caloriesPer100g, proteinPer100g, carbsPer100g, fatPer100g);
        ValidateServingSize(servingSizeGrams);

        name = name.Trim();

        Name = name;
        NormalizedName = name.ToLowerInvariant();
        Category = category;
        CaloriesPer100g = caloriesPer100g;
        ProteinPer100g = proteinPer100g;
        CarbsPer100g = carbsPer100g;
        FatPer100g = fatPer100g;
        ServingSizeGrams = servingSizeGrams;
    }

    public void Archive()
    {
        if (IsArchived)
            throw new InvalidOperationException("Food is already archived.");

        IsArchived = true;
    }

    public void Restore()
    {
        if (!IsArchived)
            throw new InvalidOperationException("Food is not archived.");

        IsArchived = false;
    }

    private void EnsureNotArchived()
    {
        if (IsArchived)
            throw new InvalidOperationException("Archived foods cannot be modified.");
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Food name cannot be empty.");
    }

    private static void ValidateMacros(
        decimal calories,
        decimal protein,
        decimal carbs,
        decimal fat)
    {
        if (calories < 0 || protein < 0 || carbs < 0 || fat < 0)
            throw new DomainException("Calories and macros cannot be negative.");
    }

    private static void ValidateServingSize(decimal? servingSizeGrams)
    {
        if (servingSizeGrams is <= 0)
            throw new DomainException("Serving size must be greater than zero.");
    }
}
