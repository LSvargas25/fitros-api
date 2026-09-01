using FitRos.Application.Abstractions.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Foods.GetFoodById;

public sealed class GetFoodByIdHandler
    : IRequestHandler<GetFoodByIdQuery, FoodDto?>
{
    private readonly IFitRosDbContext _context;

    public GetFoodByIdHandler(IFitRosDbContext context)
    {
        _context = context;
    }

    public async Task<FoodDto?> Handle(
        GetFoodByIdQuery query,
        CancellationToken cancellationToken)
    {
        return await _context.Foods
            .Where(x => x.Id == query.Id)
            .Select(x => new FoodDto(
                x.Id,
                x.Name,
                x.Category,
                x.CaloriesPer100g,
                x.ProteinPer100g,
                x.CarbsPer100g,
                x.FatPer100g,
                x.ServingSizeGrams,
                x.IsArchived))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
