using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.WeeklyTrainingPlans.Common;
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
        WeeklyTrainingPlanAccess.EnsureAuthenticated(_currentUser);

        var plan = await _context.WeeklyTrainingPlans
            .Include(p => p.Days)
            .FirstOrDefaultAsync(x => x.Id == command.PlanId, cancellationToken);

        if (plan is null)
            throw new NotFoundException("Training plan not found.");

        await WeeklyTrainingPlanAccess.EnsureCanManageAsync(_currentUser, plan, _context, cancellationToken);

        var routineExists = await _context.WorkoutRoutines
            .AnyAsync(r => r.Id == command.WorkoutRoutineId, cancellationToken);

        if (!routineExists)
            throw new NotFoundException("Workout routine not found.");

        plan.AssignRoutineToDay(command.Day, command.WorkoutRoutineId, command.Notes);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
