using FitRos.Application.Common.Security;
using FitRos.Domain.Enums;
using MediatR;

namespace FitRos.Application.Features.Users.Client.GetClientsCount;

public sealed record GetClientsCountQuery : IRequest<int>, IAuthorizeRequest
{
    public UserRole[] AllowedRoles => new[] { UserRole.OwnerApp, UserRole.Admin };
}
