namespace FitRos.Application.Features.Users.Coach.GetCoachById;

public sealed record CoachDetailDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Status,
    Guid? GymId,
    string? GymName);
