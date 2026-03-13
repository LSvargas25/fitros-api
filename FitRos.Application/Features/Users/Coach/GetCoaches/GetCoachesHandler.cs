using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Users.Coach.GetCoaches;

public sealed class GetCoachesHandler : IRequestHandler<GetCoachesQuery, List<CoachListItemDto>>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetCoachesHandler(IFitRosDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<CoachListItemDto>> Handle(
        GetCoachesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Users
            .IgnoreQueryFilters()
            .Where(u => u.Role == UserRole.Coach);

        if (_currentUser.IsAdmin())
            query = query.Where(u => u.GymId == _currentUser.GymId);

        var coaches = await query.ToListAsync(cancellationToken);

        var gymIds = coaches
            .Where(c => c.GymId.HasValue)
            .Select(c => c.GymId!.Value)
            .Distinct()
            .ToList();

        var gyms = await _context.Gyms
            .Where(g => gymIds.Contains(g.Id))
            .ToDictionaryAsync(g => g.Id, g => g.Name, cancellationToken);

        return coaches.Select(c =>
        {
            gyms.TryGetValue(c.GymId ?? Guid.Empty, out var gymName);
            return new CoachListItemDto(
                c.Id,
                c.Email,
                c.FirstName,
                c.LastName,
                c.Status.ToString(),
                c.GymId,
                c.GymId.HasValue ? gymName : null);
        }).ToList();
    }
}
