using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Application.Features.MealPlans.Common;
using FitRos.Application.Features.MealPlans.GetMealPlanById;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.MealPlans.GetActiveMealPlan;

public sealed class GetActiveMealPlanHandler
    : IRequestHandler<GetActiveMealPlanQuery, MealPlanDto?>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetActiveMealPlanHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<MealPlanDto?> Handle(
        GetActiveMealPlanQuery query,
        CancellationToken cancellationToken)
    {
        MealPlanAccess.EnsureAuthenticated(_currentUser);

        var clientProfile = await _context.ClientProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.ClientProfileId, cancellationToken);

        if (clientProfile is null)
            throw new NotFoundException("Client profile not found.");

        if (clientProfile.GymId != _currentUser.GymId && !_currentUser.IsOwner())
            throw new ForbiddenException("Client does not belong to your gym.");

        if (_currentUser.IsCoach() && clientProfile.CoachId != _currentUser.UserId)
            throw new ForbiddenException("You are not the assigned coach for this client.");

        var plan = await _context.MealPlans
            .AsNoTracking()
            .Include(p => p.Entries)
            .FirstOrDefaultAsync(
                x => x.ClientProfileId == query.ClientProfileId && x.Status == MealPlanStatus.Active,
                cancellationToken);

        if (plan is null)
            return null;

        var foods = await MealPlanQueries.LoadFoodMacrosAsync(
            _context,
            plan.Entries.Select(e => e.FoodId),
            cancellationToken);

        return MealPlanMapper.ToDto(plan, foods);
    }
}
