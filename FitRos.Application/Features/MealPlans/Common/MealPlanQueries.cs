using FitRos.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.MealPlans.Common;

internal static class MealPlanQueries
{
    /// <summary>
    /// Loads the per-100 g macros for every food referenced by the given entries,
    /// keyed by FoodId, for the mapper to expand into per-entry totals.
    /// </summary>
    public static async Task<Dictionary<Guid, FoodMacros>> LoadFoodMacrosAsync(
        IFitRosDbContext context,
        IEnumerable<Guid> foodIds,
        CancellationToken cancellationToken)
    {
        var ids = foodIds.Distinct().ToList();

        if (ids.Count == 0)
            return new Dictionary<Guid, FoodMacros>();

        return await context.Foods
            .AsNoTracking()
            .Where(f => ids.Contains(f.Id))
            .ToDictionaryAsync(
                f => f.Id,
                f => new FoodMacros(
                    f.Name,
                    f.CaloriesPer100g,
                    f.ProteinPer100g,
                    f.CarbsPer100g,
                    f.FatPer100g),
                cancellationToken);
    }
}
