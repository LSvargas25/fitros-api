using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.MealPlans.Common;
using FitRos.Application.Features.MealPlans.GetMealPlanById;
using FitRos.Application.Features.MyPlans.Common;
using FitRos.Domain.Entities.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.MyPlans.GetMyActiveMealPlan;

/// <summary>
/// The authenticated caller's single Active meal plan, or null when they have
/// none. The one-active-plan-per-client rule guarantees at most one.
/// </summary>
public record GetMyActiveMealPlanQuery : IRequest<MealPlanDto?>;

public sealed class GetMyActiveMealPlanHandler
    : IRequestHandler<GetMyActiveMealPlanQuery, MealPlanDto?>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetMyActiveMealPlanHandler(IFitRosDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<MealPlanDto?> Handle(
        GetMyActiveMealPlanQuery request,
        CancellationToken cancellationToken)
    {
        var profileId = await CallerClientProfile.ResolveIdAsync(_context, _currentUser, cancellationToken);

        var plan = await _context.MealPlans
            .AsNoTracking()
            .Include(p => p.Entries)
            .FirstOrDefaultAsync(
                p => p.ClientProfileId == profileId && p.Status == MealPlanStatus.Active,
                cancellationToken);

        if (plan is null)
            return null;

        var foods = await MealPlanQueries.LoadFoodMacrosAsync(
            _context, plan.Entries.Select(e => e.FoodId), cancellationToken);

        return MealPlanMapper.ToDto(plan, foods);
    }
}
