using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
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
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedException("User not authenticated.");

        var clientProfile = await _context.ClientProfiles
            .FirstOrDefaultAsync(x => x.Id == command.ClientProfileId, cancellationToken);

        if (clientProfile is null)
            throw new NotFoundException("Client profile not found.");

        if (clientProfile.GymId != _currentUser.GymId && !_currentUser.IsOwner())
            throw new ForbiddenException("Client does not belong to your gym.");

        if (_currentUser.IsCoach() && clientProfile.CoachId != _currentUser.UserId)
            throw new ForbiddenException("You are not the assigned coach for this client.");

        var plan = WeeklyTrainingPlan.Create(
            clientProfile.Id,
            clientProfile.CoachId ?? Guid.Empty,
            _currentUser.GymId,
            command.Name);

        _context.WeeklyTrainingPlans.Add(plan);

        await _context.SaveChangesAsync(cancellationToken);

        return new CreateWeeklyTrainingPlanResponse(plan.Id);
    }
}
