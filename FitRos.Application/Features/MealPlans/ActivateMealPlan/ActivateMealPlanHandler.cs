using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.MealPlans.Common;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.MealPlans.ActivateMealPlan;

public sealed class ActivateMealPlanHandler
    : IRequestHandler<ActivateMealPlanCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public ActivateMealPlanHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        ActivateMealPlanCommand command,
        CancellationToken cancellationToken)
    {
        MealPlanAccess.EnsureAuthenticated(_currentUser);

        var plan = await _context.MealPlans
            .FirstOrDefaultAsync(x => x.Id == command.MealPlanId, cancellationToken);

        if (plan is null)
            throw new NotFoundException("Meal plan not found.");

        MealPlanAccess.EnsureCanManage(_currentUser, plan);

        // Validate the transition first so a bad request touches nothing.
        plan.Activate();

        // One-active-plan-per-client rule: activating a plan supersedes any other
        // Active meal plan the same client holds. The previous one moves to
        // Archived (kept for history, no longer editable). Enforced here at the
        // application layer because it spans multiple aggregates. Loop handles
        // the (data-only) case of more than one pre-existing Active row.
        var superseded = await _context.MealPlans
            .Where(p => p.ClientProfileId == plan.ClientProfileId
                        && p.Id != plan.Id
                        && p.Status == MealPlanStatus.Active)
            .ToListAsync(cancellationToken);

        foreach (var previous in superseded)
            previous.Archive();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
