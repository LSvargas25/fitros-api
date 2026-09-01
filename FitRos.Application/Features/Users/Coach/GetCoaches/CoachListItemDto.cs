namespace FitRos.Application.Features.Users.Coach.GetCoaches;

public sealed record CoachListItemDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Status,
    Guid? GymId,
    string? GymName);
