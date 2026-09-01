using FitRos.Domain.Entities.Enums;

namespace FitRos.API.Contracts.MealPlans;

public record AddMealPlanEntryRequest(
    DayOfWeek Day,
    MealType Meal,
    Guid FoodId,
    decimal QuantityGrams);
