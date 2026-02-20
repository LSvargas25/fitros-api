using FitRos.Application.Abstractions.Persistence;
using FitRos.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.WorkoutRoutines.MoveExerciseInWorkoutRoutine;

public class MoveExerciseInWorkoutRoutineHandler
{
    private readonly IFitRosDbContext _context;

    public MoveExerciseInWorkoutRoutineHandler(IFitRosDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
     MoveExerciseInWorkoutRoutineCommand command,
     CancellationToken cancellationToken)
    {
        var routine = await _context.WorkoutRoutines
            .Include(r => r.Exercises)
            .FirstOrDefaultAsync(
                r => r.Id == command.WorkoutRoutineId,
                cancellationToken);

        if (routine is null)
            throw new DomainException("Workout routine not found.");

        var exercise = routine.Exercises
            .FirstOrDefault(e => e.ExerciseId == command.ExerciseId);

        if (exercise is null)
            throw new DomainException("Exercise not found in routine.");

 
        exercise.SetOrder(-999);

        await _context.SaveChangesAsync(cancellationToken);

 
        routine.MoveExercise(command.ExerciseId, command.NewOrder);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}