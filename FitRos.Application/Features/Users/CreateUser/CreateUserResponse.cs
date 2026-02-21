namespace FitRos.Application.Features.Users.CreateUser;

public sealed record CreateUserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    int Role
);