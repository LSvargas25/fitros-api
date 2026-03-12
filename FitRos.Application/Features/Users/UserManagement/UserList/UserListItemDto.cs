namespace FitRos.Application.Features.Users.UserManagement.UserList;

public sealed record UserDetailsDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    int Role,
    int Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);