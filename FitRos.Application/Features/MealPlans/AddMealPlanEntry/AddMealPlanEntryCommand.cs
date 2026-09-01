using FitRos.Domain.Entities.Enums;
using MediatR;

namespace FitRos.Application.Features.MealPlans.AddMealPlanEntry;

public record AddMealPlanEntryCommand(
    Guid MealPlanId,
    DayOfWeek Day,
    MealType Meal,
    Guid FoodId,
    decimal QuantityGrams
) : IRequest;
