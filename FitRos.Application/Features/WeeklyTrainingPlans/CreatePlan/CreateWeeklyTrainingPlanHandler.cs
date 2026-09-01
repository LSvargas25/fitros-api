using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.WeeklyTrainingPlans.Common;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.WeeklyTraining;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.WeeklyTrainingPlans.CreatePlan;

public sealed class CreateWeeklyTrainingPlanHandler
    : IRequestHandler<CreateWeeklyTrainingPlanCommand, CreateWeeklyTrainingPlanResponse>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public CreateWeeklyTrainingPlanHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<CreateWeeklyTrainingPlanResponse> Handle(
        CreateWeeklyTrainingPlanCommand command,
        CancellationToken cancellationToken)
    {
        WeeklyTrainingPlanAccess.EnsureAuthenticated(_currentUser);

        var clientProfile = await _context.ClientProfiles
            .FirstOrDefaultAsync(x => x.Id == command.ClientProfileId, cancellationToken);

        if (clientProfile is null)
            throw new NotFoundException("Client profile not found.");

        WeeklyTrainingPlanAccess.EnsureCanAccessClient(_currentUser, clientProfile);

        var plan = WeeklyTrainingPlan.Create(
            clientProfile.Id,
            clientProfile.CoachId,
            clientProfile.GymId,
            command.Name);

        _context.WeeklyTrainingPlans.Add(plan);

        await _context.SaveChangesAsync(cancellationToken);

        return new CreateWeeklyTrainingPlanResponse(plan.Id);
    }
}
