using FitRos.Application.Common.Security;
using FitRos.Domain.Entities.Users;
using MediatR;

namespace FitRos.Application.Features.Users.GetUserById;

public sealed record GetUserByIdQuery(Guid Id)
    : IRequest<UserDetailsDto>, ITenantGuardedRequest
{
    public Guid ResourceId => Id;

    public Type EntityType => typeof(User);
}