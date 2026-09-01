using FitRos.Application.Common.Security;
using FitRos.Application.Features.Users.UserManagement.CreateUser;
using FitRos.Domain.Enums;
using MediatR;

namespace FitRos.Application.Features.Users.Client.CreateClient;

public sealed record CreateClientCommand(
    string Email,
    string FirstName,
    string LastName,
    string Password,
    Guid? GymId = null
) : IRequest<CreateUserResponse>, IAuthorizeRequest
{
    public UserRole[] AllowedRoles =>
        new[] { UserRole.OwnerApp, UserRole.Admin, UserRole.Coach };
}