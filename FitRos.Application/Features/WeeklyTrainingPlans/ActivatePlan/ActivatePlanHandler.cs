using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.WeeklyTrainingPlans.Common;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.WeeklyTrainingPlans.ActivatePlan;

public sealed class ActivatePlanHandler : IRequestHandler<ActivatePlanCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public ActivatePlanHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(ActivatePlanCommand command, CancellationToken cancellationToken)
    {
        WeeklyTrainingPlanAccess.EnsureAuthenticated(_currentUser);

        var plan = await _context.WeeklyTrainingPlans
            .FirstOrDefaultAsync(x => x.Id == command.PlanId, cancellationToken);

        if (plan is null)
            throw new NotFoundException("Training plan not found.");

        await WeeklyTrainingPlanAccess.EnsureCanManageAsync(_currentUser, plan, _context, cancellationToken);

        var otherActivePlans = await _context.WeeklyTrainingPlans
            .Where(x =>
                x.ClientProfileId == plan.ClientProfileId &&
                x.Id != plan.Id &&
                x.Status == TrainingPlanStatus.Active)
            .ToListAsync(cancellationToken);

        foreach (var otherPlan in otherActivePlans)
            otherPlan.Archive();

        plan.Activate();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
