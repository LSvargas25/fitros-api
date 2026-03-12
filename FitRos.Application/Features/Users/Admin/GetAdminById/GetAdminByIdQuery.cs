using FitRos.Application.Common.Security;
using FitRos.Domain.Enums;
using MediatR;

namespace FitRos.Application.Features.Users.Admin.GetAdminById;

public sealed record GetAdminByIdQuery(Guid AdminId) : IRequest<AdminDetailDto?>, IAuthorizeRequest
{
    public UserRole[] AllowedRoles => new[] { UserRole.OwnerApp };
}
