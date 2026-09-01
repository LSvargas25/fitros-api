using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.WeeklyTrainingPlans.Common;
using FitRos.Application.Features.WeeklyTrainingPlans.GetPlanById;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.WeeklyTrainingPlans.GetActivePlan;

public sealed class GetActiveTrainingPlanHandler
    : IRequestHandler<GetActiveTrainingPlanQuery, WeeklyTrainingPlanDto?>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetActiveTrainingPlanHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<WeeklyTrainingPlanDto?> Handle(
        GetActiveTrainingPlanQuery query,
        CancellationToken cancellationToken)
    {
        WeeklyTrainingPlanAccess.EnsureAuthenticated(_currentUser);

        var clientProfile = await _context.ClientProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.ClientProfileId, cancellationToken);

        if (clientProfile is null)
            throw new NotFoundException("Client profile not found.");

        WeeklyTrainingPlanAccess.EnsureCanAccessClient(_currentUser, clientProfile);

        var plan = await _context.WeeklyTrainingPlans
            .AsNoTracking()
            .Include(p => p.Days)
            .FirstOrDefaultAsync(
                x => x.ClientProfileId == query.ClientProfileId && x.Status == TrainingPlanStatus.Active,
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
