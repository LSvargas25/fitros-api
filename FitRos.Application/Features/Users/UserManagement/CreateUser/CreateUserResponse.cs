namespace FitRos.Application.Features.Users.UserManagement.CreateUser;

public sealed record CreateUserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    int Role
);