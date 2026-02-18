using FitRos.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.WorkoutRoutines.AddExerciseToWorkoutRoutine;

public class AddExerciseToWorkoutRoutineHandler
{
    private readonly IFitRosDbContext _context;

    public AddExerciseToWorkoutRoutineHandler(IFitRosDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        AddExerciseToWorkoutRoutineCommand command,
        CancellationToken cancellationToken)
    {
        // 1️⃣ Verificar que la rutina exista
        var routine = await _context.WorkoutRoutines
            .Include(r => r.Exercises)
            .FirstOrDefaultAsync(r => r.Id == command.WorkoutRoutineId, cancellationToken);

        if (routine is null)
            return false;

        var exerciseExists = await _context.Exercises
            .AnyAsync(e => e.Id == command.ExerciseId, cancellationToken);

        if (!exerciseExists)
            throw new InvalidOperationException("The specified exercise does not exist.");

        routine.AddExercise(
            command.ExerciseId,
            command.Order,
            command.SuggestedSets,
            command.SuggestedReps,
            command.SuggestedRestSeconds);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
