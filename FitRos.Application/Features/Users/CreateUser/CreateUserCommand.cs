using FitRos.Domain.Enums;

namespace FitRos.Application.Features.Users.CreateUser;

public sealed record CreateUserCommand(
    string Email,
    string FirstName,
    string LastName,
    string Password
);