using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Application.Features.MealPlans.Common;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Nutrition;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.MealPlans.CreateMealPlan;

public sealed class CreateMealPlanHandler
    : IRequestHandler<CreateMealPlanCommand, CreateMealPlanResponse>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public CreateMealPlanHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<CreateMealPlanResponse> Handle(
        CreateMealPlanCommand command,
        CancellationToken cancellationToken)
    {
        MealPlanAccess.EnsureAuthenticated(_currentUser);

        var clientProfile = await _context.ClientProfiles
            .FirstOrDefaultAsync(x => x.Id == command.ClientProfileId, cancellationToken);

        if (clientProfile is null)
            throw new NotFoundException("Client profile not found.");

        if (clientProfile.GymId != _currentUser.GymId && !_currentUser.IsOwner())
            throw new ForbiddenException("Client does not belong to your gym.");

        if (_currentUser.IsCoach() && clientProfile.CoachId != _currentUser.UserId)
            throw new ForbiddenException("You are not the assigned coach for this client.");

        var plan = MealPlan.Create(
            clientProfile.Id,
            clientProfile.CoachId ?? Guid.Empty,
            _currentUser.GymId,
            command.Name);

        _context.MealPlans.Add(plan);

        await _context.SaveChangesAsync(cancellationToken);

        return new CreateMealPlanResponse(plan.Id);
    }
}
