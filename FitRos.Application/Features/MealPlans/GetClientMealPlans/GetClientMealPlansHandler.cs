using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Application.Features.MealPlans.Common;
using FitRos.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.MealPlans.GetClientMealPlans;

public sealed class GetClientMealPlansHandler
    : IRequestHandler<GetClientMealPlansQuery, List<MealPlanListItemDto>>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetClientMealPlansHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<MealPlanListItemDto>> Handle(
        GetClientMealPlansQuery query,
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

        return await _context.MealPlans
            .AsNoTracking()
            .Where(p => p.ClientProfileId == query.ClientProfileId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new MealPlanListItemDto(
                p.Id,
                p.Name,
                p.Status,
                p.Entries.Count,
                p.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
