using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.MealPlans.Common;
using FitRos.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.MealPlans.UpdateMealPlanEntry;

public sealed class UpdateMealPlanEntryHandler
    : IRequestHandler<UpdateMealPlanEntryCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public UpdateMealPlanEntryHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        UpdateMealPlanEntryCommand command,
        CancellationToken cancellationToken)
    {
        MealPlanAccess.EnsureAuthenticated(_currentUser);

        var plan = await _context.MealPlans
            .Include(p => p.Entries)
            .FirstOrDefaultAsync(x => x.Id == command.MealPlanId, cancellationToken);

        if (plan is null)
            throw new NotFoundException("Meal plan not found.");

        MealPlanAccess.EnsureCanManage(_currentUser, plan);

        plan.UpdateEntry(command.EntryId, command.QuantityGrams);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
