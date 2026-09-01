namespace FitRos.Application.Features.Users.Client.GetClients;

public sealed record ClientUserListItemDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Status,
    Guid? gymId);
