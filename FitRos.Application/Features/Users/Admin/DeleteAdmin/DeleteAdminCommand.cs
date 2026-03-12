using FitRos.Application.Common.Security;
using FitRos.Domain.Enums;
using MediatR;

namespace FitRos.Application.Features.Users.Admin.DeleteAdmin;

public sealed record DeleteAdminCommand(Guid AdminId)
    : IRequest, IAuthorizeRequest
{
    public UserRole[] AllowedRoles => new[] { UserRole.OwnerApp };
}
