using FitRos.Domain.Entities.Enums;

namespace FitRos.Application.Features.MealPlans.GetClientMealPlans;

public record MealPlanListItemDto(
    Guid Id,
    string Name,
    MealPlanStatus Status,
    int EntryCount,
    DateTime CreatedAt);
