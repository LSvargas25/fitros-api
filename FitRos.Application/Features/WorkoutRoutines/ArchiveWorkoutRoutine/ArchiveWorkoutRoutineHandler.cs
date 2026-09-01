using FitRos.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using FitRos.Domain.Common;


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
            throw new DomainException("Workout routine not found.");

        routine.Archive();

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
