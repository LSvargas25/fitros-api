using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Notifications.GetNotifications;

public sealed class GetNotificationsHandler
    : IRequestHandler<GetNotificationsQuery, List<NotificationDto>>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetNotificationsHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<NotificationDto>> Handle(
        GetNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedException("User not authenticated.");

        var userId = _currentUser.UserId!.Value;

        return await _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto(
                n.Id,
                n.UserId,
                n.Title,
                n.Message,
                n.Type,
                n.ReferenceId,
                n.IsRead,
                n.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
