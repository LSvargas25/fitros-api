using FitRos.Application.Abstractions.Persistence;
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
        Guid id,
        CancellationToken cancellationToken)
    {
        var routine = await _context.WorkoutRoutines
            .Where(r => r.Id == id)
            .Select(r => new WorkoutRoutineDetailsDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                Version = r.Version,
                Status = (int)r.Status
            })
            .FirstOrDefaultAsync(cancellationToken);

        return routine;
    }
}
