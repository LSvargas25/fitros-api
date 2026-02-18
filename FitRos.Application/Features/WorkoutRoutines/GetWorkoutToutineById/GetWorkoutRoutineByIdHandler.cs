using FitRos.Application.Abstractions.Persistence;
using FitRos.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.WorkoutRoutines.GetWorkoutRoutineById;
public class GetWorkoutRoutineByIdHandler
{
    private readonly IFitRosDbContext _context;

    public GetWorkoutRoutineByIdHandler(IFitRosDbContext context)
    {
        _context = context;
    }

    public async Task<WorkoutRoutineDetailsDto?> Handle(
        GetWorkoutRoutineByIdQuery query,
        CancellationToken cancellationToken)
    {
        var routine = await _context.WorkoutRoutines
            .Where(x => x.Id == query.Id)
            .Select(x => new WorkoutRoutineDetailsDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                Status = (int)x.Status,
                Version = x.Version
            })
            .FirstOrDefaultAsync(cancellationToken);
         
        return routine;
    }
}
