using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.WorkoutRoutines.GetWorkoutRoutines;

public class GetWorkoutRoutinesHandler
{
    private readonly IFitRosDbContext _context;

    public GetWorkoutRoutinesHandler(IFitRosDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<WorkoutRoutineListItem>> Handle(
        GetWorkoutRoutinesQuery query,
        CancellationToken cancellationToken)
    {
        var routinesQuery = _context.WorkoutRoutines
            .AsNoTracking()
            .AsQueryable();

        if (query.Status.HasValue)
        {
            routinesQuery = routinesQuery
                .Where(r => r.Status == query.Status.Value);
        }

        var totalCount = await routinesQuery.CountAsync(cancellationToken);

        var items = await routinesQuery
            .OrderBy(r => r.Name)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new WorkoutRoutineListItem
            {
                Id = r.Id,
                Name = r.Name,
                Version = r.Version,
                Status = r.Status.ToString()
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<WorkoutRoutineListItem>
        {
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
            Items = items
        };
    }
}
