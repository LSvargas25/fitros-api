using FitRos.Application.Common.Security;
using FitRos.Application.Features.Users.UserManagement.CreateUser;
using FitRos.Domain.Enums;
using MediatR;

namespace FitRos.Application.Features.Users.Admin.CreateAdmin;

public sealed record CreateAdminCommand(
    string Email,
    string FirstName,
    string LastName,
    string Password
) : IRequest<CreateUserResponse>, IAuthorizeRequest
{
    public UserRole[] AllowedRoles => new[] { UserRole.OwnerApp };
}
