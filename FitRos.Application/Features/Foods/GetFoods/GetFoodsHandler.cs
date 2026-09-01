using FitRos.Application.Abstractions.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Foods.GetFoods;

public sealed class GetFoodsHandler
    : IRequestHandler<GetFoodsQuery, List<FoodListItemDto>>
{
    private readonly IFitRosDbContext _context;

    public GetFoodsHandler(IFitRosDbContext context)
    {
        _context = context;
    }

    public async Task<List<FoodListItemDto>> Handle(
        GetFoodsQuery query,
        CancellationToken cancellationToken)
    {
        var foods = _context.Foods.AsQueryable();

        if (!query.IncludeArchived)
            foods = foods.Where(x => !x.IsArchived);

        if (query.Category.HasValue)
            foods = foods.Where(x => x.Category == query.Category.Value);

        return await foods
            .OrderBy(x => x.Name)
            .Select(x => new FoodListItemDto(
                x.Id,
                x.Name,
                x.Category,
                x.CaloriesPer100g,
                x.ProteinPer100g,
                x.CarbsPer100g,
                x.FatPer100g))
            .ToListAsync(cancellationToken);
    }
}
