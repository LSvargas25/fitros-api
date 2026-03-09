using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Security;
using FitRos.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.WeeklyTrainingPlans.GetPlanById;

public sealed class GetWeeklyTrainingPlanByIdHandler
    : IRequestHandler<GetWeeklyTrainingPlanByIdQuery, WeeklyTrainingPlanDto>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetWeeklyTrainingPlanByIdHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<WeeklyTrainingPlanDto> Handle(
        GetWeeklyTrainingPlanByIdQuery query,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedException("User not authenticated.");

        var plan = await _context.WeeklyTrainingPlans
            .AsNoTracking()
            .Include(p => p.Days)
            .FirstOrDefaultAsync(x => x.Id == query.PlanId, cancellationToken);

        if (plan is null)
            throw new NotFoundException("Training plan not found.");

        if (plan.GymId != _currentUser.GymId && !_currentUser.IsOwner())
            throw new ForbiddenException("Training plan does not belong to your gym.");

        if (_currentUser.IsCoach() && plan.CoachId != _currentUser.UserId)
            throw new ForbiddenException("You are not the assigned coach for this training plan.");

        var routineIds = plan.Days.Select(d => d.WorkoutRoutineId).ToList();

        var routineNames = await _context.WorkoutRoutines
            .AsNoTracking()
            .Where(r => routineIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.Name, cancellationToken);

        var days = plan.Days
            .OrderBy(d => d.Day)
            .Select(d => new TrainingPlanDayDto(
                d.Id,
                d.Day,
                d.WorkoutRoutineId,
                routineNames.GetValueOrDefault(d.WorkoutRoutineId, string.Empty),
                d.Notes))
            .ToList();

        return new WeeklyTrainingPlanDto(
            plan.Id,
            plan.ClientProfileId,
            plan.CoachId,
            plan.Name,
            plan.Status,
            plan.CreatedAt,
            days);
    }
}
