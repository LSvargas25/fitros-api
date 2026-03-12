namespace FitRos.Application.Features.Gyms.GetGymById;

public sealed record GymDetailDto(
    Guid Id,
    string Name,
    string Address,
    string PhoneNumber,
    bool IsActive,
    bool IsDeleted,
    Guid? AdminId,
    string? AdminEmail,
    string? AdminName);
