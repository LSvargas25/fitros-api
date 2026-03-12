using FitRos.Application.Common.Security;
using FitRos.Domain.Enums;
using MediatR;

namespace FitRos.Application.Features.Users.Client.GetClients;

public sealed record GetClientsQuery : IRequest<List<ClientUserListItemDto>>, IAuthorizeRequest
{
    public UserRole[] AllowedRoles => new[] { UserRole.OwnerApp, UserRole.Admin, UserRole.Coach };
}
