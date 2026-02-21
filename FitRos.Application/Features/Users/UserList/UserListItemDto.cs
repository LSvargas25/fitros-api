namespace FitRos.Application.Features.Users.GetUserById;

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