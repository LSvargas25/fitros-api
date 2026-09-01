using MediatR;

namespace FitRos.Application.Features.Notifications.MarkNotificationAsRead;

public record MarkNotificationAsReadCommand(Guid NotificationId) : IRequest;
