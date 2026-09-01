using FitRos.Domain.Entities.Enums;

namespace FitRos.Application.Features.Notifications.GetNotifications;

public record NotificationDto(
    Guid Id,
    Guid UserId,
    string Title,
    string Message,
    NotificationType Type,
    Guid? ReferenceId,
    bool IsRead,
    DateTime CreatedAt
);
