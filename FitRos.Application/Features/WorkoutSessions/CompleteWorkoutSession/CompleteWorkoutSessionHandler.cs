using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Application.Features.WorkoutSessions;
using FitRos.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.WorkoutSessions.CompleteWorkoutSession;

public sealed class CompleteWorkoutSessionHandler : IRequestHandler<CompleteWorkoutSessionCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public CompleteWorkoutSessionHandler(IFitRosDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(CompleteWorkoutSessionCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            throw new UnauthorizedException("User not authenticated.");

        var session = await _context.WorkoutSessions
            .IncludeSets()
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken);

        if (session is null)
            throw new NotFoundException("Workout session not found.");

        if (session.UserId != _currentUser.UserId!.Value && !_currentUser.IsOwner() && !_currentUser.IsAdmin())
            throw new ForbiddenException("You do not own this workout session.");

        session.Complete();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
