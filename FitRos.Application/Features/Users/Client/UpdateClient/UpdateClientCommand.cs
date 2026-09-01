using FitRos.Application.Common.Security;
using FitRos.Application.Features.Users.Client.GetClientById;
using FitRos.Domain.Enums;
using MediatR;

namespace FitRos.Application.Features.Users.Client.UpdateClient;

public sealed record UpdateClientCommand(
    Guid ClientId,
    string? FirstName,
    string? LastName,
    string? Email) : IRequest<ClientUserDetailDto>, IAuthorizeRequest
{
    public UserRole[] AllowedRoles => new[] { UserRole.OwnerApp, UserRole.Admin, UserRole.Coach };
}
