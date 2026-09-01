using FitRos.Application.Common.Security;
using FitRos.Domain.Enums;
using MediatR;

namespace FitRos.Application.Features.Users.UserManagement.DeleteUser;

public sealed record DeleteUserCommand(Guid UserId)
    : IRequest, IAuthorizeRequest
{
    public UserRole[] AllowedRoles =>
        new[]
        {
            UserRole.OwnerApp,
            UserRole.Admin,
            UserRole.Coach
        };
}