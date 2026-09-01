using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.MealPlans.Common;
using FitRos.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.MealPlans.AddMealPlanEntry;

public sealed class AddMealPlanEntryHandler
    : IRequestHandler<AddMealPlanEntryCommand>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public AddMealPlanEntryHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        AddMealPlanEntryCommand command,
        CancellationToken cancellationToken)
    {
        MealPlanAccess.EnsureAuthenticated(_currentUser);

        var plan = await _context.MealPlans
            .Include(p => p.Entries)
            .FirstOrDefaultAsync(x => x.Id == command.MealPlanId, cancellationToken);

        if (plan is null)
            throw new NotFoundException("Meal plan not found.");

        MealPlanAccess.EnsureCanManage(_currentUser, plan);

        var foodExists = await _context.Foods
            .AnyAsync(f => f.Id == command.FoodId && !f.IsArchived, cancellationToken);

        if (!foodExists)
            throw new NotFoundException("Food not found.");

        plan.AddEntry(command.Day, command.Meal, command.FoodId, command.QuantityGrams);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
