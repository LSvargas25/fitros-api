using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
 

namespace FitRos.Application.Features.Users.DeleteUser;

public sealed class DeleteUserHandler
    : IRequestHandler<DeleteUserCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public DeleteUserHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteUserCommand command, CancellationToken ct)
    {
        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == command.UserId, ct);

        if (user is null)
            throw new NotFoundException("User not found.");

        if (_currentUser.UserId == user.Id)
            throw new DomainException("You cannot delete yourself.");

        if (user.Status != UserStatus.Inactive)
            throw new DomainException("User must be deactivated before permanent deletion.");

        ValidatePermissions(user);

        await ValidateCriticalRoles(user, ct);

        await ValidateDependencies(user, ct);

        _context.Remove(user);

        await _context.SaveChangesAsync(ct);
    }

    private void ValidatePermissions(User target)
    {
        if (_currentUser.IsOwner())
            return;

        if (_currentUser.IsAdmin())
        {
            if (target.Role == UserRole.OwnerApp)
                throw new ForbiddenException("Admin cannot delete OwnerApp.");

            return;
        }

        if (_currentUser.IsCoach())
        {
            if (target.Role != UserRole.Client)
                throw new ForbiddenException("Coach can only delete Client users.");

            return;
        }

        throw new ForbiddenException("You are not authorized to delete users.");
    }

    private async Task ValidateCriticalRoles(User target, CancellationToken ct)
    {
        if (target.Role == UserRole.OwnerApp)
            throw new ForbiddenException("OwnerApp cannot be deleted.");

        if (target.Role == UserRole.Admin)
        {
            var adminCount = await _context.Users
                .IgnoreQueryFilters()
                .CountAsync(u => u.Role == UserRole.Admin, ct);

            if (adminCount == 1)
                throw new DomainException("Cannot delete the last Admin.");
        }

        if (target.Role == UserRole.Coach)
        {
            var coachCount = await _context.Users
                .IgnoreQueryFilters()
                .CountAsync(u => u.Role == UserRole.Coach, ct);

            if (coachCount == 1)
                throw new DomainException("Cannot delete the only Coach.");
        }
    }

    private async Task ValidateDependencies(User target, CancellationToken ct)
    {
        if (target.Role == UserRole.Client)
        {
            var hasSessions = await _context.WorkoutSessions
                .AnyAsync(x => x.UserId == target.Id, ct);

            if (hasSessions)
                throw new DomainException("Cannot delete user with related workout sessions.");
        }
    }
}