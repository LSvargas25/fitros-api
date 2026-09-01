using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Application.Features.WorkoutSessions;
using FitRos.Domain.Common;
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
            .AsNoTracking()
            .IncludeSets()
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken);

        if (session is null)
            return null;

        if (session.UserId != _currentUser.UserId!.Value
            && !_currentUser.IsOwner()
            && !_currentUser.IsAdmin()
            && !_currentUser.IsCoach())
        {
            throw new ForbiddenException("You do not have access to this workout session.");
        }

        var routine = await _context.WorkoutRoutines
            .AsNoTracking()
            .Include(r => r.Exercises)
            .FirstOrDefaultAsync(r => r.Id == session.RoutineId, cancellationToken);

        return WorkoutSessionMapper.ToDetailsDto(session, routine);
    }
}
