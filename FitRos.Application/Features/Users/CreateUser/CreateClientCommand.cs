using FitRos.Application.Common.Security;
using FitRos.Domain.Enums;
using MediatR;

namespace FitRos.Application.Features.Users.CreateUser;

public sealed record CreateClientCommand(
    string Email,
    string FirstName,
    string LastName,
    string Password
) : IRequest<CreateUserResponse>, IAuthorizeRequest
{
    public UserRole[] AllowedRoles =>
        new[] { UserRole.OwnerApp, UserRole.Admin, UserRole.Coach };
}