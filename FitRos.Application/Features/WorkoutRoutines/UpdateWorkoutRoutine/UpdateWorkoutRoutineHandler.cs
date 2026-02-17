using FitRos.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.WorkoutRoutines.UpdateWorkoutRoutine;

public class UpdateWorkoutRoutineHandler
{
    private readonly IFitRosDbContext _context;

    public UpdateWorkoutRoutineHandler(IFitRosDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        Guid id,
        UpdateWorkoutRoutineCommand command,
        CancellationToken cancellationToken)
    {
        var routine = await _context.WorkoutRoutines
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (routine is null)
            return false;

        routine.UpdateDetails(command.Name, command.Description);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
