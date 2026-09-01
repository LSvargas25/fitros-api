using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.ClientProfiles.GetByCoach;

public sealed class GetClientsByCoachQueryHandler
    : IRequestHandler<GetClientsByCoachQuery, IReadOnlyCollection<ClientListItemDto>>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetClientsByCoachQueryHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyCollection<ClientListItemDto>> Handle(
        GetClientsByCoachQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedException("User not authenticated.");

        ValidatePermissions(request.CoachId);

        var query = _context.ClientProfiles
            .IgnoreQueryFilters()
            .Where(x => x.CoachId == request.CoachId);

        if (!_currentUser.IsOwner())
            query = query.Where(x => x.GymId == _currentUser.GymId);

        var clients = await query
            .Select(x => new ClientListItemDto
            {
                Id = x.Id,
                UserId = x.UserId,
                Status = x.Status,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return clients;
    }

    private void ValidatePermissions(Guid coachId)
    {
        if (_currentUser.IsOwner() || _currentUser.IsAdmin())
            return;

        if (_currentUser.IsCoach() && _currentUser.UserId == coachId)
            return;

        throw new ForbiddenException("You are not authorized to access these clients.");
    }
}