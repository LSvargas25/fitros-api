namespace FitRos.Application.Features.Gyms.GetAllGyms;

public sealed record GymListItemDto(
    Guid Id,
    string Name,
    string Address,
    string PhoneNumber,
    bool IsActive,
    List<GymAdminDto> Admins);

public sealed record GymAdminDto(
    Guid Id,
    string FullName);