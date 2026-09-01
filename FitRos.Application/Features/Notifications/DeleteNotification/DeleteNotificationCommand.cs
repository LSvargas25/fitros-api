using MediatR;

namespace FitRos.Application.Features.Notifications.DeleteNotification;

public record DeleteNotificationCommand(Guid NotificationId) : IRequest;
