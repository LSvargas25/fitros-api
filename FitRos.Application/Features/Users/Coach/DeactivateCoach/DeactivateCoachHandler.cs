using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Users.Coach.DeactivateCoach;

public sealed class DeactivateCoachHandler : IRequestHandler<DeactivateCoachCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public DeactivateCoachHandler(IFitRosDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(DeactivateCoachCommand command, CancellationToken ct)
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

        if (_currentUser.UserId == coach.Id)
            throw new DomainException("You cannot deactivate yourself.");

        coach.Deactivate();

        await _context.SaveChangesAsync(ct);
    }
}
