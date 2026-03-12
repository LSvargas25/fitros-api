namespace FitRos.Application.Features.Users.Client.GetClientById;

public sealed record ClientUserDetailDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Status,
    Guid? GymId,
    Guid? ClientProfileId,
    Guid? CoachId,
    string? CoachName);
