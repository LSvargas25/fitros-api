using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Features.WorkoutRoutines.GetWorkoutRoutineById;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.WorkoutRoutines.GetLatestWorkoutRoutine;

public sealed class GetLatestWorkoutRoutineHandler
{
    private readonly IFitRosDbContext _context;

    public GetLatestWorkoutRoutineHandler(IFitRosDbContext context)
    {
        _context = context;
    }

    public async Task<WorkoutRoutineDetailsDto?> Handle(
        GetLatestWorkoutRoutineQuery query,
        CancellationToken cancellationToken)
    {
        var routine = await _context.WorkoutRoutines
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == query.WorkoutRoutineId, cancellationToken);

        if (routine is null)
            return null;

        var latest = await _context.WorkoutRoutines
            .AsNoTracking()
            .Include(r => r.Exercises)
            .Where(r => r.RoutineGroupId == routine.RoutineGroupId)
            .OrderByDescending(r => r.Version)
            .FirstAsync(cancellationToken);

        return new WorkoutRoutineDetailsDto(
            latest.Id,
            latest.Name,
            latest.Description,
            latest.Version,
            latest.Status,
            latest.Exercises
                .OrderBy(e => e.Order)
                .Select(e => new WorkoutRoutineExerciseDto(
                    e.ExerciseId,
                    e.Order,
                    e.SuggestedSets,
                    e.SuggestedReps,
                    e.SuggestedRestSeconds))
                .ToList()
        );
    }
}