using FitRos.Application.Common.Security;
using FitRos.Domain.Enums;
using MediatR;

namespace FitRos.Application.Features.Users.Client.GetClientById;

public sealed record GetClientByIdQuery(Guid ClientId) : IRequest<ClientUserDetailDto?>, IAuthorizeRequest
{
    public UserRole[] AllowedRoles => new[] { UserRole.OwnerApp, UserRole.Admin, UserRole.Coach };
}
