using MediatR;

namespace FitRos.Application.Features.Foods.GetFoodById;

public record GetFoodByIdQuery(Guid Id) : IRequest<FoodDto?>;
