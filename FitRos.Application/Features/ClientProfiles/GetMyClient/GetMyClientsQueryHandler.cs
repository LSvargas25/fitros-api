using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Application.Features.ClientProfiles.GetMyClient;
using FitRos.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.ClientProfiles.GetMyClients;

public sealed class GetMyClientsQueryHandler
    : IRequestHandler<GetMyClientsQuery, IReadOnlyList<MyClientListItemResponse>>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetMyClientsQueryHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<MyClientListItemResponse>> Handle(
        GetMyClientsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedException("User is not authenticated.");

        if (_currentUser.Role == Domain.Enums.UserRole.Client)
            throw new ForbiddenException("You are not authorized.");

        var baseQuery = _context.ClientProfiles.AsNoTracking();

        if (_currentUser.IsCoach())
        {
            var coachId = _currentUser.UserId!.Value;
            baseQuery = baseQuery.Where(cp => cp.CoachId == coachId);
        }

        var query =
            from cp in baseQuery
            join u in _context.Users.AsNoTracking() on cp.UserId equals u.Id
            select new
            {
                cp,
                u,
                LastMeasure = _context.PhysicalMeasures.AsNoTracking()
                    .Where(m => m.ClientProfileId == cp.Id)
                    .OrderByDescending(m => m.RecordedAt)
                    .Select(m => new { m.RecordedAt, m.Weight })
                    .FirstOrDefault()
            };

        var result = await query
            .OrderBy(x => x.u.LastName)
            .ThenBy(x => x.u.FirstName)
            .Select(x => new MyClientListItemResponse(
                x.cp.Id,
                x.u.Id,
                x.u.Email,
                x.u.FirstName,
                x.u.LastName,
                x.cp.CreatedAt,
                x.LastMeasure != null ? x.LastMeasure.RecordedAt : null,
                x.LastMeasure != null ? x.LastMeasure.Weight : null
            ))
            .ToListAsync(cancellationToken);

        return result;
    }
}