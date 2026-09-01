using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.WorkoutSessions;
using FitRos.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.WorkoutSessions.GetMyWorkoutSessionHistory;

public sealed class GetMyWorkoutSessionHistoryHandler
    : IRequestHandler<GetMyWorkoutSessionHistoryQuery, List<WorkoutSessionListItemDto>>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetMyWorkoutSessionHistoryHandler(IFitRosDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<WorkoutSessionListItemDto>> Handle(
        GetMyWorkoutSessionHistoryQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            throw new UnauthorizedException("User not authenticated.");

        var from = (request.From ?? DateTime.UtcNow.Date.AddMonths(-1)).Date;
        var to = (request.To ?? DateTime.UtcNow.Date).Date;

        var sessions = await _context.WorkoutSessions
            .AsNoTracking()
            .IncludeSets()
            .Where(s => s.UserId == _currentUser.UserId!.Value
                && s.ScheduledDate >= from
                && s.ScheduledDate <= to)
            .OrderByDescending(s => s.ScheduledDate)
            .ToListAsync(cancellationToken);

        return sessions
            .Select(s => new WorkoutSessionListItemDto(
                s.Id,
                s.ScheduledDate,
                s.RoutineNameSnapshot,
                s.Status,
                s.Sets.Count))
            .ToList();
    }
}
