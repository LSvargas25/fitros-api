using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Notifications.DeleteNotification;

public sealed class DeleteNotificationHandler
    : IRequestHandler<DeleteNotificationCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public DeleteNotificationHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        DeleteNotificationCommand command,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedException("User not authenticated.");

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == command.NotificationId, cancellationToken);

        if (notification is null)
            throw new NotFoundException("Notification not found.");

        if (notification.UserId != _currentUser.UserId)
            throw new ForbiddenException("You do not have access to this notification.");

        _context.Remove(notification);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
