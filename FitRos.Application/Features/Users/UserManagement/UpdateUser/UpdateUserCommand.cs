using FitRos.Application.Common.Security;
using FitRos.Application.Features.Users.UserManagement.GetUsersAdvanced;
using FitRos.Domain.Enums;
using MediatR;

namespace FitRos.Application.Features.Users.UserManagement.UpdateUser;

public sealed record UpdateUserCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string? Email,
    UserRole? Role
) : IRequest<UserListItemResponse>, IAuthorizeRequest
{
    public UserRole[] AllowedRoles =>
        new[]
        {
            UserRole.OwnerApp,
            UserRole.Admin,
            UserRole.Coach
        };
}