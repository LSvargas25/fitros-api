using FitRos.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.WorkoutRoutines.PublishWorkoutRoutine;

public class PublishWorkoutRoutineHandler
{
    private readonly IFitRosDbContext _context;

    public PublishWorkoutRoutineHandler(IFitRosDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        PublishWorkoutRoutineCommand command,
        CancellationToken cancellationToken)
    {
        var routine = await _context.WorkoutRoutines
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (routine is null)
            return false;

        routine.Publish();

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
