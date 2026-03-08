using MediatR;

namespace FitRos.Application.Features.Notifications.GetNotifications;

public record GetNotificationsQuery() : IRequest<List<NotificationDto>>;
