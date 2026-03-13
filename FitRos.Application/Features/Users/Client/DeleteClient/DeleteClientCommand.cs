using FitRos.Application.Common.Security;
using FitRos.Domain.Enums;
using MediatR;

namespace FitRos.Application.Features.Users.Client.DeleteClient;

public sealed record DeleteClientCommand(Guid ClientId) : IRequest, IAuthorizeRequest
{
    public UserRole[] AllowedRoles => new[] { UserRole.OwnerApp, UserRole.Admin, UserRole.Coach };
}
