using FitRos.Application.Abstractions.Persistence;
using FitRos.Domain.Common;
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
        var routine = await _context.WorkoutRoutines
        .Include("_exercises")
        .FirstOrDefaultAsync(r => r.Id == command.WorkoutRoutineId, cancellationToken);


        if (routine is null)
            throw new DomainException("Workout routine not found.");

        var exerciseExists = await _context.Exercises
            .AnyAsync(e => e.Id == command.ExerciseId, cancellationToken);

        if (!exerciseExists)
            throw new DomainException("The specified exercise does not exist.");

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
