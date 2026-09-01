using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.WorkoutSessions.SkipWorkoutSession;

public sealed class SkipWorkoutSessionHandler : IRequestHandler<SkipWorkoutSessionCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public SkipWorkoutSessionHandler(IFitRosDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(SkipWorkoutSessionCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            throw new UnauthorizedException("User not authenticated.");

        var session = await _context.WorkoutSessions
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken);

        if (session is null)
            throw new NotFoundException("Workout session not found.");

        if (session.UserId != _currentUser.UserId!.Value && !_currentUser.IsOwner() && !_currentUser.IsAdmin())
            throw new ForbiddenException("You do not own this workout session.");

        session.Skip();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
