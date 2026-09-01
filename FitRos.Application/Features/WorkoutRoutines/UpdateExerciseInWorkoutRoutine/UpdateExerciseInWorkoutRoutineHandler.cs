using FitRos.Application.Abstractions.Persistence;
using FitRos.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.WorkoutRoutines.UpdateExerciseInWorkoutRoutine;

/// <summary>
/// Changes the target sets / reps / rest of an exercise already in a routine,
/// without removing and re-adding it (which would reset its position).
/// The aggregate enforces Draft-only.
/// </summary>
public class UpdateExerciseInWorkoutRoutineHandler
{
    private readonly IFitRosDbContext _context;

    public UpdateExerciseInWorkoutRoutineHandler(IFitRosDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        UpdateExerciseInWorkoutRoutineCommand command,
        CancellationToken cancellationToken)
    {
        var routine = await _context.WorkoutRoutines
            .IgnoreQueryFilters()
            .Include(r => r.Exercises)
            .FirstOrDefaultAsync(
                r => r.Id == command.WorkoutRoutineId,
                cancellationToken);

        if (routine is null)
            throw new DomainException("Workout routine not found.");

        routine.UpdateExercise(
            command.ExerciseId,
            command.SuggestedSets,
            command.SuggestedReps,
            command.SuggestedRestSeconds);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
