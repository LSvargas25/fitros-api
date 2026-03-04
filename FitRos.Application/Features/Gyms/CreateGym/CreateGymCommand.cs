namespace FitRos.Application.Features.Gyms.CreateGym;

public record CreateGymCommand(
    string Name,
    string Address,
    string PhoneNumber,

    Guid? ExistingAdminUserId,

    string? AdminEmail,
    string? AdminFirstName,
    string? AdminLastName,
    string? AdminPassword
);