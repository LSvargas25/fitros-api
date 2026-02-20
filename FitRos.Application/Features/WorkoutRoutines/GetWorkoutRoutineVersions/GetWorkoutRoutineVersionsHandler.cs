using FitRos.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.WorkoutRoutines.GetWorkoutRoutineVersions;

public sealed class GetWorkoutRoutineVersionsHandler
{
    private readonly IFitRosDbContext _context;

    public GetWorkoutRoutineVersionsHandler(IFitRosDbContext context)
    {
        _context = context;
    }

    public async Task<List<WorkoutRoutineVersionListItem>> Handle(
        GetWorkoutRoutineVersionsQuery query,
        CancellationToken cancellationToken)
    {
        var routine = await _context.WorkoutRoutines
            .AsNoTracking()
            .Where(r => r.Id == query.WorkoutRoutineId)
            .Select(r => new { r.RoutineGroupId })
            .FirstOrDefaultAsync(cancellationToken);

        if (routine is null)
            throw new KeyNotFoundException("Workout routine not found.");

        var versions = await _context.WorkoutRoutines
            .AsNoTracking()
            .Where(r => r.RoutineGroupId == routine.RoutineGroupId)
            .OrderByDescending(r => r.Version)
            .Select(r => new WorkoutRoutineVersionListItem(
                r.Id,
                r.RoutineGroupId,
                r.Version,
                r.Status,
                r.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return versions;
    }
}