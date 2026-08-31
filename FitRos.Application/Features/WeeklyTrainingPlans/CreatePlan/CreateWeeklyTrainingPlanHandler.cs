using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
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

        // A coach may also create a plan for their own personal profile
        // (Kind == CoachSelf, whose UserId is the coach's own user id).
        var isOwnProfile = clientProfile.UserId == _currentUser.UserId;

        // A CoachSelf profile carries no CoachId; the coach is the coach of their
        // own plan, so stamp their user id.
        // An independent client's plan keeps a null CoachId.
        var planCoachId = clientProfile.CoachId
            ?? (isOwnProfile && _currentUser.IsCoach() ? _currentUser.UserId : (Guid?)null);

        var plan = WeeklyTrainingPlan.Create(
            clientProfile.Id,
            planCoachId,
            clientProfile.GymId,
            command.Name);

        _context.WeeklyTrainingPlans.Add(plan);

        await _context.SaveChangesAsync(cancellationToken);

        return new CreateWeeklyTrainingPlanResponse(plan.Id);
    }
}
