using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Notifications.MarkNotificationAsRead;

public sealed class MarkNotificationAsReadHandler
    : IRequestHandler<MarkNotificationAsReadCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public MarkNotificationAsReadHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        MarkNotificationAsReadCommand command,
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

        notification.MarkAsRead();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
