using FitRos.Domain.Entities.Enums;

namespace FitRos.Application.Features.MealPlans.GetMealPlanById;

public record MealPlanEntryDto(
    Guid Id,
    DayOfWeek Day,
    MealType Meal,
    Guid FoodId,
    string FoodName,
    decimal QuantityGrams,
    decimal Calories,
    decimal Protein,
    decimal Carbs,
    decimal Fat);
