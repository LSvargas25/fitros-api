using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Training;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.WorkoutSessions.RemoveSetFromWorkoutSession;

public sealed class RemoveSetFromWorkoutSessionHandler : IRequestHandler<RemoveSetFromWorkoutSessionCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public RemoveSetFromWorkoutSessionHandler(IFitRosDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(RemoveSetFromWorkoutSessionCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            throw new UnauthorizedException("User not authenticated.");

        var session = await _context.WorkoutSessions
            .IncludeSets()
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken);

        if (session is null)
            throw new NotFoundException("Workout session not found.");

        EnsureOwnership(session);

        session.RemoveSet(request.SetId);

        await _context.SaveChangesAsync(cancellationToken);
    }

    private void EnsureOwnership(WorkoutSession session)
    {
        if (session.UserId == _currentUser.UserId!.Value)
            return;

        if (_currentUser.IsOwner() || _currentUser.IsAdmin())
            return;

        throw new ForbiddenException("You do not own this workout session.");
    }
}
