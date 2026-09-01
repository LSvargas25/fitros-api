using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.MealPlans.Common;
using FitRos.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.MealPlans.GetMealPlanById;

public sealed class GetMealPlanByIdHandler
    : IRequestHandler<GetMealPlanByIdQuery, MealPlanDto>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetMealPlanByIdHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<MealPlanDto> Handle(
        GetMealPlanByIdQuery query,
        CancellationToken cancellationToken)
    {
        MealPlanAccess.EnsureAuthenticated(_currentUser);

        var plan = await _context.MealPlans
            .AsNoTracking()
            .Include(p => p.Entries)
            .FirstOrDefaultAsync(x => x.Id == query.MealPlanId, cancellationToken);

        if (plan is null)
            throw new NotFoundException("Meal plan not found.");

        MealPlanAccess.EnsureCanManage(_currentUser, plan);

        var foods = await MealPlanQueries.LoadFoodMacrosAsync(
            _context,
            plan.Entries.Select(e => e.FoodId),
            cancellationToken);

        return MealPlanMapper.ToDto(plan, foods);
    }
}
