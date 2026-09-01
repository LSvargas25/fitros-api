using FitRos.Domain.Common;
using FitRos.Domain.Entities.Enums;

namespace FitRos.Domain.Entities.Nutrition;

public sealed class MealPlanEntry
{
    public Guid Id { get; private set; }

    public Guid MealPlanId { get; private set; }

    public DayOfWeek Day { get; private set; }

    public MealType Meal { get; private set; }

    public Guid FoodId { get; private set; }

    public decimal QuantityGrams { get; private set; }

    private MealPlanEntry() { }

    internal static MealPlanEntry Create(
        Guid mealPlanId,
        DayOfWeek day,
        MealType meal,
        Guid foodId,
        decimal quantityGrams)
    {
        ValidateQuantity(quantityGrams);

        return new MealPlanEntry
        {
            Id = Guid.NewGuid(),
            MealPlanId = mealPlanId,
            Day = day,
            Meal = meal,
            FoodId = foodId,
            QuantityGrams = quantityGrams,
        };
    }

    internal void UpdateQuantity(decimal quantityGrams)
    {
        ValidateQuantity(quantityGrams);
        QuantityGrams = quantityGrams;
    }

    private static void ValidateQuantity(decimal quantityGrams)
    {
        if (quantityGrams <= 0)
            throw new DomainException("Quantity must be greater than zero.");
    }
}
