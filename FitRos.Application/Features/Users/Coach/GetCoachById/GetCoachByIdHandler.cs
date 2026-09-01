using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Users.Coach.GetCoachById;

public sealed class GetCoachByIdHandler : IRequestHandler<GetCoachByIdQuery, CoachDetailDto?>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetCoachByIdHandler(IFitRosDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<CoachDetailDto?> Handle(
        GetCoachByIdQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Users
            .IgnoreQueryFilters()
            .Where(u => u.Id == request.CoachId && u.Role == UserRole.Coach);

        if (_currentUser.IsAdmin())
            query = query.Where(u => u.GymId == _currentUser.GymId);

        var coach = await query.FirstOrDefaultAsync(cancellationToken);

        if (coach is null)
            return null;

        string? gymName = null;
        if (coach.GymId.HasValue)
        {
            var gym = await _context.Gyms
                .FirstOrDefaultAsync(g => g.Id == coach.GymId.Value, cancellationToken);
            gymName = gym?.Name;
        }

        return new CoachDetailDto(
            coach.Id,
            coach.Email,
            coach.FirstName,
            coach.LastName,
            coach.Status.ToString(),
            coach.GymId,
            gymName);
    }
}
