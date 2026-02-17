using FitRos.Application.Abstractions.Persistence;
using FitRos.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.WorkoutRoutines.GetWorkoutRoutines;

public class GetWorkoutRoutinesHandler
{
    private readonly IFitRosDbContext _context;

    public GetWorkoutRoutinesHandler(IFitRosDbContext context)
    {
        _context = context;
    }

    public async Task<List<WorkoutRoutineListItem>> Handle(
        GetWorkoutRoutinesQuery query,
        CancellationToken cancellationToken)
    {
        var routinesQuery = _context.WorkoutRoutines
            .AsNoTracking()
            .AsQueryable();

        // 🔹 Filtro opcional por estado
        if (query.Status.HasValue)
        {
            routinesQuery = routinesQuery
                .Where(r => r.Status == query.Status.Value);
        }

        return await routinesQuery
            .Select(r => new WorkoutRoutineListItem
            {
                Id = r.Id,
                Name = r.Name,
                Version = r.Version,
                Status = r.Status.ToString()
            })
            .ToListAsync(cancellationToken);
    }
}
