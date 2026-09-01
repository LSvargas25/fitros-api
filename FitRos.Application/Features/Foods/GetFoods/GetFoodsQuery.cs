using FitRos.Domain.Entities.Enums;
using MediatR;

namespace FitRos.Application.Features.Foods.GetFoods;

public record GetFoodsQuery(
    FoodCategory? Category,
    bool IncludeArchived = false
) : IRequest<List<FoodListItemDto>>;
