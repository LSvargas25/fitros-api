using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Application.Features.WorkoutSessions;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Training;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.WorkoutSessions.GetWorkoutSessionById;

public sealed class GetWorkoutSessionByIdHandler
    : IRequestHandler<GetWorkoutSessionByIdQuery, WorkoutSessionDetailsDto?>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetWorkoutSessionByIdHandler(IFitRosDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<WorkoutSessionDetailsDto?> Handle(
        GetWorkoutSessionByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            throw new UnauthorizedException("User not authenticated.");

        var session = await _context.WorkoutSessions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .IncludeSets()
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken);

        if (session is null)
            return null;

        await EnsureCanAccessAsync(session, cancellationToken);

        var routine = await _context.WorkoutRoutines
            .AsNoTracking()
            .Include(r => r.Exercises)
            .FirstOrDefaultAsync(r => r.Id == session.RoutineId, cancellationToken);

        return WorkoutSessionMapper.ToDetailsDto(session, routine);
    }

    private async Task EnsureCanAccessAsync(WorkoutSession session, CancellationToken cancellationToken)
    {
        if (session.UserId == _currentUser.UserId!.Value)
            return;

        if (_currentUser.IsOwner())
            return;

        if (session.GymId != _currentUser.GymId)
            throw new ForbiddenException("You do not have access to this workout session.");

        if (_currentUser.IsAdmin())
            return;

        if (_currentUser.IsCoach())
        {
            var isResponsibleCoach = await _context.ClientProfiles
                .IgnoreQueryFilters()
                .AnyAsync(
                    c => c.UserId == session.UserId && c.CoachId == _currentUser.UserId,
                    cancellationToken);

            if (isResponsibleCoach)
                return;
        }

        throw new ForbiddenException("You do not have access to this workout session.");
    }
}
