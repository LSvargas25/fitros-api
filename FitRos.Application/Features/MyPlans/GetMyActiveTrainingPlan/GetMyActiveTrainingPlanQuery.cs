using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.MyPlans.Common;
using FitRos.Application.Features.WeeklyTrainingPlans.GetPlanById;
using FitRos.Domain.Entities.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.MyPlans.GetMyActiveTrainingPlan;

/// <summary>
/// The authenticated caller's single Active weekly training plan, or null when
/// they have none. The one-active-plan-per-client rule guarantees at most one.
/// </summary>
public record GetMyActiveTrainingPlanQuery : IRequest<WeeklyTrainingPlanDto?>;

public sealed class GetMyActiveTrainingPlanHandler
    : IRequestHandler<GetMyActiveTrainingPlanQuery, WeeklyTrainingPlanDto?>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetMyActiveTrainingPlanHandler(IFitRosDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<WeeklyTrainingPlanDto?> Handle(
        GetMyActiveTrainingPlanQuery request,
        CancellationToken cancellationToken)
    {
        var profileId = await CallerClientProfile.ResolveIdAsync(_context, _currentUser, cancellationToken);

        var plan = await _context.WeeklyTrainingPlans
            .AsNoTracking()
            .Include(p => p.Days)
            .FirstOrDefaultAsync(
                p => p.ClientProfileId == profileId && p.Status == TrainingPlanStatus.Active,
                cancellationToken);

        if (plan is null)
            return null;

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
