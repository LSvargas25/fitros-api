using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using DomainUser = FitRos.Domain.Entities.Users.User;

namespace FitRos.Application.Features.Users.Coach.DeleteCoach;

public sealed class DeleteCoachHandler : IRequestHandler<DeleteCoachCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public DeleteCoachHandler(IFitRosDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteCoachCommand command, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedException("User not authenticated.");

        var coach = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == command.CoachId && u.Role == UserRole.Coach, ct);

        if (coach is null)
            throw new NotFoundException("Coach not found.");

        if (_currentUser.UserId == coach.Id)
            throw new DomainException("You cannot delete yourself.");

        if (coach.Status != UserStatus.Inactive)
            throw new DomainException("Coach must be deactivated before permanent deletion.");

        _context.Remove(coach);

        await _context.SaveChangesAsync(ct);
    }
}
