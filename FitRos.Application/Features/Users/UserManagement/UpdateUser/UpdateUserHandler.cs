using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Application.Features.Users.UserManagement.GetUsersAdvanced;
using FitRos.Domain.Common;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;


namespace FitRos.Application.Features.Users.UserManagement.UpdateUser;

public sealed class UpdateUserHandler
    : IRequestHandler<UpdateUserCommand, UserListItemResponse>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public UpdateUserHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<UserListItemResponse> Handle(
        UpdateUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

        if (user is null)
            throw new NotFoundException("User not found.");

        ValidatePermissions(user);

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var normalized = request.Email.Trim().ToUpperInvariant();

            var exists = await _context.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.NormalizedEmail == normalized && u.Id != user.Id,
                          cancellationToken);

            if (exists)
                throw new DomainException("Email already in use.");

            user.ChangeEmail(request.Email);
        }

        if (request.Role.HasValue && request.Role.Value != user.Role)
        {
            
            if (!_currentUser.IsOwner() && !_currentUser.IsAdmin())
                throw new ForbiddenException("You are not authorized.");

            if (user.Role == UserRole.OwnerApp)
                throw new ForbiddenException("OwnerApp role cannot be changed.");

            if (user.Role == UserRole.Admin &&
                request.Role.Value != UserRole.Admin)
            {
                var adminCount = await _context.Users
                    .IgnoreQueryFilters()
                    .CountAsync(u => u.Role == UserRole.Admin, cancellationToken);

                if (adminCount == 1)
                    throw new DomainException("Cannot downgrade the last Admin.");
            }

            var hasSessions = await _context.WorkoutSessions
       .IgnoreQueryFilters()
       .AnyAsync(s => s.UserId == user.Id, cancellationToken);

            if (hasSessions)
                throw new DomainException("User with sessions cannot change role.");

            user.ChangeRole(request.Role.Value);
        }

        user.UpdateBasicInfo(request.FirstName, request.LastName);

        await _context.SaveChangesAsync(cancellationToken);

        return new UserListItemResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            (int)user.Role,
            (int)user.Status,
            user.CreatedAt);
    }

    private void ValidatePermissions(Domain.Entities.Users.User target)
    {
        if (_currentUser.IsOwner())
            return;

        if (_currentUser.IsAdmin())
        {
            if (target.Role == UserRole.OwnerApp)
                throw new ForbiddenException("Admin cannot modify OwnerApp.");

            return;
        }

        if (_currentUser.IsCoach())
        {
            if (target.Role != UserRole.Client)
                throw new ForbiddenException("Coach can only modify Client users.");

            return;
        }

        if (_currentUser.UserId != target.Id)
            throw new ForbiddenException("You are not authorized to modify this user.");
    }
}