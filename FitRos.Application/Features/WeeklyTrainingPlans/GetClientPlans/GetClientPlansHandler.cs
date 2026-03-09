using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.WeeklyTrainingPlans.GetClientPlans;

public sealed class GetClientPlansHandler
    : IRequestHandler<GetClientPlansQuery, List<TrainingPlanListItemDto>>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetClientPlansHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<TrainingPlanListItemDto>> Handle(
        GetClientPlansQuery query,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedException("User not authenticated.");

        var clientProfile = await _context.ClientProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.ClientProfileId, cancellationToken);

        if (clientProfile is null)
            throw new NotFoundException("Client profile not found.");

        if (clientProfile.GymId != _currentUser.GymId && !_currentUser.IsOwner())
            throw new ForbiddenException("Client does not belong to your gym.");

        if (_currentUser.IsCoach() && clientProfile.CoachId != _currentUser.UserId)
            throw new ForbiddenException("You are not the assigned coach for this client.");

        return await _context.WeeklyTrainingPlans
            .AsNoTracking()
            .Where(p => p.ClientProfileId == query.ClientProfileId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new TrainingPlanListItemDto(
                p.Id,
                p.Name,
                p.Status,
                p.Days.Count,
                p.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
