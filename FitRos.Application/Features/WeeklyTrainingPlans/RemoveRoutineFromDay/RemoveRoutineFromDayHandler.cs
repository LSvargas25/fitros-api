using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.WeeklyTrainingPlans.Common;
using FitRos.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.WeeklyTrainingPlans.RemoveRoutineFromDay;

public sealed class RemoveRoutineFromDayHandler : IRequestHandler<RemoveRoutineFromDayCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public RemoveRoutineFromDayHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(RemoveRoutineFromDayCommand command, CancellationToken cancellationToken)
    {
        WeeklyTrainingPlanAccess.EnsureAuthenticated(_currentUser);

        var plan = await _context.WeeklyTrainingPlans
            .Include(p => p.Days)
            .FirstOrDefaultAsync(x => x.Id == command.PlanId, cancellationToken);

        if (plan is null)
            throw new NotFoundException("Training plan not found.");

        await WeeklyTrainingPlanAccess.EnsureCanManageAsync(_currentUser, plan, _context, cancellationToken);

        plan.RemoveRoutineFromDay(command.Day);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
