using FitRos.Domain.Entities.Enums;

namespace FitRos.Application.Features.MealPlans.GetMealPlanById;

public record MealPlanDto(
    Guid Id,
    Guid ClientProfileId,
    Guid CoachId,
    string Name,
    MealPlanStatus Status,
    DateTime CreatedAt,
    decimal TotalCalories,
    decimal TotalProtein,
    decimal TotalCarbs,
    decimal TotalFat,
    List<MealPlanEntryDto> Entries);
