using FitRos.Application.Abstractions.Persistence;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Training;
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
        var context = (DbContext)_context;

        var routine = await context.Set<WorkoutRoutine>()
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

        var originalOrder = exercise.Order;
        var newOrder = command.NewOrder;

        // Validate BEFORE any mutation (important because we use a temporary order value)
        if (newOrder <= 0)
            throw new DomainException("New order must be greater than zero.");

        if (newOrder > routine.Exercises.Count)
            throw new DomainException("New order exceeds the number of exercises.");

        if (originalOrder == newOrder)
            return true;

        using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        // Move selected exercise to a temporary safe value (avoid unique index collision)
        exercise.SetOrder(-9999);
        await context.SaveChangesAsync(cancellationToken);

        // Shift other exercises
        if (originalOrder < newOrder)
        {
            foreach (var e in routine.Exercises
                .Where(e => e.Order > originalOrder && e.Order <= newOrder))
            {
                e.SetOrder(e.Order - 1);
            }
        }
        else
        {
            foreach (var e in routine.Exercises
                .Where(e => e.Order >= newOrder && e.Order < originalOrder))
            {
                e.SetOrder(e.Order + 1);
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        // Set final order
        exercise.SetOrder(newOrder);
        await context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return true;
    }
}