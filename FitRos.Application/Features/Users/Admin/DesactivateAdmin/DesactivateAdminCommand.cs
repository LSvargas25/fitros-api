using FitRos.Application.Common.Security;
using FitRos.Domain.Enums;
using MediatR;

namespace FitRos.Application.Features.Users.Admin.DesactivateAdmin;

public sealed record DesactivateAdminCommand(Guid AdminId)
    : IRequest, IAuthorizeRequest
{
    public UserRole[] AllowedRoles => new[] { UserRole.OwnerApp };
}
