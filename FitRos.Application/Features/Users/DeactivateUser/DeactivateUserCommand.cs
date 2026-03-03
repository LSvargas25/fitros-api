using FitRos.Application.Common.Security;
using FitRos.Domain.Enums;
using MediatR;

namespace FitRos.Application.Features.Users.DeactivateUser;

public sealed record DeactivateUserCommand(Guid UserId)
    : IRequest, IAuthorizeRequest
{
    public UserRole[] AllowedRoles =>
        new[] { UserRole.OwnerApp, UserRole.Admin };
}