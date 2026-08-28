using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.MealPlans.Common;
using FitRos.Application.Features.MealPlans.GetMealPlanById;
using FitRos.Application.Features.MyPlans.Common;
using FitRos.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.MyPlans.GetMyMealPlanById;

public record GetMyMealPlanByIdQuery(Guid PlanId) : IRequest<MealPlanDto>;

public sealed class GetMyMealPlanByIdHandler
    : IRequestHandler<GetMyMealPlanByIdQuery, MealPlanDto>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetMyMealPlanByIdHandler(IFitRosDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<MealPlanDto> Handle(
        GetMyMealPlanByIdQuery request,
        CancellationToken cancellationToken)
    {
        var profileId = await CallerClientProfile.ResolveIdAsync(_context, _currentUser, cancellationToken);

        var plan = await _context.MealPlans
            .AsNoTracking()
            .Include(p => p.Entries)
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken);

        // 404 (not 403) for a plan that isn't the caller's — don't reveal it exists.
        if (plan is null || plan.ClientProfileId != profileId)
            throw new NotFoundException("Meal plan not found.");

        var foods = await MealPlanQueries.LoadFoodMacrosAsync(
            _context, plan.Entries.Select(e => e.FoodId), cancellationToken);

        return MealPlanMapper.ToDto(plan, foods);
    }
}
