using FitRos.Application.Features.MealPlans.GetMealPlanById;
using FitRos.Domain.Entities.Nutrition;

namespace FitRos.Application.Features.MealPlans.Common;

internal readonly record struct FoodMacros(
    string Name,
    decimal CaloriesPer100g,
    decimal ProteinPer100g,
    decimal CarbsPer100g,
    decimal FatPer100g);

internal static class MealPlanMapper
{
    public static MealPlanDto ToDto(
        MealPlan plan,
        IReadOnlyDictionary<Guid, FoodMacros> foods)
    {
        var entries = plan.Entries
            .OrderBy(e => e.Day)
            .ThenBy(e => e.Meal)
            .Select(e =>
            {
                var macros = foods.TryGetValue(e.FoodId, out var m) ? m : default;
                var factor = e.QuantityGrams / 100m;

                return new MealPlanEntryDto(
                    e.Id,
                    e.Day,
                    e.Meal,
                    e.FoodId,
                    macros.Name ?? string.Empty,
                    e.QuantityGrams,
                    Round(macros.CaloriesPer100g * factor),
                    Round(macros.ProteinPer100g * factor),
                    Round(macros.CarbsPer100g * factor),
                    Round(macros.FatPer100g * factor));
            })
            .ToList();

        return new MealPlanDto(
            plan.Id,
            plan.ClientProfileId,
            plan.CoachId,
            plan.Name,
            plan.Status,
            plan.CreatedAt,
            Round(entries.Sum(e => e.Calories)),
            Round(entries.Sum(e => e.Protein)),
            Round(entries.Sum(e => e.Carbs)),
            Round(entries.Sum(e => e.Fat)),
            entries);
    }

    private static decimal Round(decimal value) => Math.Round(value, 2);
}
