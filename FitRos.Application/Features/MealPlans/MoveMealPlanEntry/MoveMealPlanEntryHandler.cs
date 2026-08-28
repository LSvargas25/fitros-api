using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.MealPlans.Common;
using FitRos.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.MealPlans.MoveMealPlanEntry;

public sealed class MoveMealPlanEntryHandler
    : IRequestHandler<MoveMealPlanEntryCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public MoveMealPlanEntryHandler(IFitRosDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        MoveMealPlanEntryCommand command,
        CancellationToken cancellationToken)
    {
        MealPlanAccess.EnsureAuthenticated(_currentUser);

        var plan = await _context.MealPlans
            .Include(p => p.Entries)
            .FirstOrDefaultAsync(x => x.Id == command.MealPlanId, cancellationToken);

        if (plan is null)
            throw new NotFoundException("Meal plan not found.");

        MealPlanAccess.EnsureCanManage(_currentUser, plan);

        plan.MoveEntry(command.EntryId, command.NewOrder);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
