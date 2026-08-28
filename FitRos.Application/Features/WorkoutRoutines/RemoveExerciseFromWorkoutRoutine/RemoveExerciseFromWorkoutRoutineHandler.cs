using FitRos.Application.Abstractions.Persistence;
using FitRos.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.WorkoutRoutines.RemoveExerciseFromWorkoutRoutine;

public class RemoveExerciseFromWorkoutRoutineHandler
    : IRequestHandler<RemoveExerciseFromWorkoutRoutineCommand, bool>
{
    private readonly IFitRosDbContext _context;

    public RemoveExerciseFromWorkoutRoutineHandler(IFitRosDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        RemoveExerciseFromWorkoutRoutineCommand command,
        CancellationToken cancellationToken)
    {
        var routine = await _context.WorkoutRoutines
            .Include(r => r.Exercises)
            .FirstOrDefaultAsync(
                r => r.Id == command.WorkoutRoutineId,
                cancellationToken);

        if (routine is null)
            throw new DomainException("Workout routine not found.");

        routine.RemoveExercise(command.ExerciseId);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
