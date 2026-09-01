using FitRos.Application.Features.Notifications.GetNotifications;
using MediatR;

namespace FitRos.Application.Features.Notifications.GetUnreadNotifications;

public record GetUnreadNotificationsQuery() : IRequest<List<NotificationDto>>;
