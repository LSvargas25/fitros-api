using FitRos.Application.Common.Security;
using FitRos.Application.Features.Users.UserManagement.UserList;
using FitRos.Domain.Entities.Users;
using MediatR;
using DomainUser = FitRos.Domain.Entities.Users.User;

namespace FitRos.Application.Features.Users.UserManagement.UserList;

public sealed record GetUserByIdQuery(Guid Id)
    : IRequest<UserDetailsDto>, ITenantGuardedRequest
{
    public Guid ResourceId => Id;

    public Type EntityType => typeof(DomainUser);
}