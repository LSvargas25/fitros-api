using FitRos.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.WorkoutRoutines.ArchiveWorkoutRoutine;

public class ArchiveWorkoutRoutineHandler
{
    private readonly IFitRosDbContext _context;

    public ArchiveWorkoutRoutineHandler(IFitRosDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        ArchiveWorkoutRoutineCommand command,
        CancellationToken cancellationToken)
    {
        var routine = await _context.WorkoutRoutines
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (routine is null)
            return false;

        routine.Archive();

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
