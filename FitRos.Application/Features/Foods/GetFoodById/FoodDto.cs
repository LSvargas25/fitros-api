using FitRos.Domain.Entities.Enums;

namespace FitRos.Application.Features.Foods.GetFoodById;

public record FoodDto(
    Guid Id,
    string Name,
    FoodCategory Category,
    decimal CaloriesPer100g,
    decimal ProteinPer100g,
    decimal CarbsPer100g,
    decimal FatPer100g,
    decimal? ServingSizeGrams,
    bool IsArchived);
