using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Entities.Notifications;

namespace FitRos.Application.Common;

public static class NotificationFactory
{
    public static Notification GymCreated(Guid recipientUserId, Guid gymId, string gymName) =>
        Notification.Create(recipientUserId, null, "New Gym Created",
            $"Gym '{gymName}' was created.", NotificationType.GymCreated, gymId);

    public static Notification AdminCreated(Guid recipientUserId, string adminName) =>
        Notification.Create(recipientUserId, null, "New Admin Created",
            $"{adminName} was registered as admin.", NotificationType.AdminCreated, null);

    public static Notification AdminAssignedToGym(Guid recipientUserId, string adminName, string gymName) =>
        Notification.Create(recipientUserId, null, "Admin Assigned to Gym",
            $"{adminName} was assigned to gym '{gymName}'.", NotificationType.AdminAssignedToGym, null);

    public static Notification ClientCreated(Guid recipientUserId, Guid? gymId, string clientName) =>
        Notification.Create(recipientUserId, gymId, "New Client Created",
            $"{clientName} was registered as a client.", NotificationType.ClientCreated, null);

    public static Notification CoachCreated(Guid recipientUserId, Guid? gymId, string coachName) =>
        Notification.Create(recipientUserId, gymId, "New Coach Created",
            $"{coachName} was registered as a coach.", NotificationType.CoachCreated, null);
}
