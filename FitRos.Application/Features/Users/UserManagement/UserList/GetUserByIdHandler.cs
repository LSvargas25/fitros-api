using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using DomainUser = FitRos.Domain.Entities.Users.User;

namespace FitRos.Application.Features.Users.UserManagement.UserList;

public sealed class GetUserByIdHandler
    : IRequestHandler<GetUserByIdQuery, UserDetailsDto>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetUserByIdHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<UserDetailsDto> Handle(
      GetUserByIdQuery query,
      CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedException("User not authenticated.");

        var user = await _context.Users
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == query.Id, ct);

        if (user is null)
            throw new NotFoundException("User not found.");

        ValidatePermissions(user);

        return new UserDetailsDto(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            (int)user.Role,
            (int)user.Status,
            user.CreatedAt,
            user.UpdatedAt);
    }

    // Mirrors ApplyUserVisibility / GetClientByIdHandler: Owner sees anyone,
    // Admin only non-Owner users in their own gym, Coach only Clients in their
    // gym, everyone else only themselves.
    private void ValidatePermissions(DomainUser target)
    {
        if (_currentUser.IsOwner())
            return;

        if (_currentUser.IsAdmin())
        {
            if (target.Role == UserRole.OwnerApp || target.GymId != _currentUser.GymId)
                throw new ForbiddenException("You are not authorized to view this user.");

            return;
        }

        if (_currentUser.IsCoach())
        {
            if (target.Role != UserRole.Client || target.GymId != _currentUser.GymId)
                throw new ForbiddenException("You are not authorized to view this user.");

            return;
        }

        if (target.Id != _currentUser.UserId)
            throw new ForbiddenException("You are not authorized to view this user.");
    }
}