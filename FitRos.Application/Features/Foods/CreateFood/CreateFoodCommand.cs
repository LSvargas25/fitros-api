using FitRos.Domain.Entities.Enums;
using MediatR;

namespace FitRos.Application.Features.Foods.CreateFood;

public record CreateFoodCommand(
    string Name,
    FoodCategory Category,
    decimal CaloriesPer100g,
    decimal ProteinPer100g,
    decimal CarbsPer100g,
    decimal FatPer100g,
    decimal? ServingSizeGrams
) : IRequest<CreateFoodResponse>;
