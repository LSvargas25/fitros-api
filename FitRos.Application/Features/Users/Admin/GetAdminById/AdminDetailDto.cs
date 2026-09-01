namespace FitRos.Application.Features.Users.Admin.GetAdminById;

public sealed record AdminDetailDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Status,
    Guid? GymId,
    string? GymName);
