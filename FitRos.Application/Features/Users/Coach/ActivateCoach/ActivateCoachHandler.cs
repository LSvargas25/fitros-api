using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Users.Coach.ActivateCoach;

public sealed class ActivateCoachHandler : IRequestHandler<ActivateCoachCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public ActivateCoachHandler(IFitRosDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(ActivateCoachCommand command, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedException("User not authenticated.");

        var query = _context.Users
            .IgnoreQueryFilters()
            .Where(u => u.Id == command.CoachId && u.Role == UserRole.Coach);

        if (_currentUser.IsAdmin())
            query = query.Where(u => u.GymId == _currentUser.GymId);

        var coach = await query.FirstOrDefaultAsync(ct);

        if (coach is null)
            throw new NotFoundException("Coach not found.");

        coach.Activate();

        await _context.SaveChangesAsync(ct);
    }
}
