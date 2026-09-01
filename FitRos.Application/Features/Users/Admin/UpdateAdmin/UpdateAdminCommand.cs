using FitRos.Application.Common.Security;
using FitRos.Application.Features.Users.Admin.GetAdminById;
using FitRos.Domain.Enums;
using MediatR;

namespace FitRos.Application.Features.Users.Admin.UpdateAdmin;

public sealed record UpdateAdminCommand(
    Guid AdminId,
    string? FirstName,
    string? LastName,
    string? Email) : IRequest<AdminDetailDto>, IAuthorizeRequest
{
    public UserRole[] AllowedRoles => new[] { UserRole.OwnerApp };
}
