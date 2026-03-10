using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.WeeklyTrainingPlans.AssignRoutineToDay;

public sealed class AssignRoutineToDayHandler : IRequestHandler<AssignRoutineToDayCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public AssignRoutineToDayHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(AssignRoutineToDayCommand command, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedException("User not authenticated.");

        var plan = await _context.WeeklyTrainingPlans
            .Include(p => p.Days)
            .FirstOrDefaultAsync(x => x.Id == command.PlanId, cancellationToken);

        if (plan is null)
            throw new NotFoundException("Training plan not found.");

        if (plan.GymId != _currentUser.GymId && !_currentUser.IsOwner())
            throw new ForbiddenException("Training plan does not belong to your gym.");

        if (_currentUser.IsCoach() && plan.CoachId != _currentUser.UserId)
            throw new ForbiddenException("You are not the assigned coach for this training plan.");

        var routineExists = await _context.WorkoutRoutines
            .AnyAsync(r => r.Id == command.WorkoutRoutineId, cancellationToken);

        if (!routineExists)
            throw new NotFoundException("Workout routine not found.");

        plan.AssignRoutineToDay(command.Day, command.WorkoutRoutineId, command.Notes);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
