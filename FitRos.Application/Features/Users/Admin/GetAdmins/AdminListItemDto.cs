namespace FitRos.Application.Features.Users.Admin.GetAdmins;

public sealed record AdminListItemDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Status,
    Guid? GymId,
    string? GymName);
