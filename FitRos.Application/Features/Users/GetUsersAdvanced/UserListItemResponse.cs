namespace FitRos.Application.Features.Users.GetUsersAdvanced;

public sealed record UserListItemResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    int Role,
    int Status,
    DateTime CreatedAt
);