using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.MyPlans.Common;
using FitRos.Application.Features.WeeklyTrainingPlans.GetPlanById;
using FitRos.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.MyPlans.GetMyTrainingPlanById;

public record GetMyTrainingPlanByIdQuery(Guid PlanId) : IRequest<WeeklyTrainingPlanDto>;

public sealed class GetMyTrainingPlanByIdHandler
    : IRequestHandler<GetMyTrainingPlanByIdQuery, WeeklyTrainingPlanDto>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetMyTrainingPlanByIdHandler(IFitRosDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<WeeklyTrainingPlanDto> Handle(
        GetMyTrainingPlanByIdQuery request,
        CancellationToken cancellationToken)
    {
        var profileId = await CallerClientProfile.ResolveIdAsync(_context, _currentUser, cancellationToken);

        var plan = await _context.WeeklyTrainingPlans
            .AsNoTracking()
            .Include(p => p.Days)
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken);

        // 404 (not 403) for a plan that isn't the caller's — don't reveal it exists.
        if (plan is null || plan.ClientProfileId != profileId)
            throw new NotFoundException("Training plan not found.");

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
