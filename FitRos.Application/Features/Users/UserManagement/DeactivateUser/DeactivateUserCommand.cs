using FitRos.Application.Common.Security;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using MediatR;
using DomainUser = FitRos.Domain.Entities.Users.User;
namespace FitRos.Application.Features.Users.UserManagement.DeactivateUser;

public sealed record DeactivateUserCommand(Guid UserId)
    : IRequest, IAuthorizeRequest, ITenantGuardedRequest
{
    public UserRole[] AllowedRoles =>
        new[] { UserRole.OwnerApp, UserRole.Admin };

    public Guid ResourceId => UserId;

    public Type EntityType => typeof(DomainUser);
}