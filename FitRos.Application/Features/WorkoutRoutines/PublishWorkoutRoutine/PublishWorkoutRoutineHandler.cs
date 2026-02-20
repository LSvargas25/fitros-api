using FitRos.Application.Abstractions.Persistence;
using FitRos.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.WorkoutRoutines.PublishWorkoutRoutine;

public sealed class PublishWorkoutRoutineHandler
{
    private readonly IFitRosDbContext _context;

    public PublishWorkoutRoutineHandler(IFitRosDbContext context)
    {
        _context = context;
    }

    public async Task Handle(
        PublishWorkoutRoutineCommand command,
        CancellationToken cancellationToken)
    {
        var routine = await _context.WorkoutRoutines
            .Include(r => r.Exercises)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (routine is null)
            throw new KeyNotFoundException("Workout routine not found.");

        //   Only the latest version in the group can be published.
        var maxVersionInGroup = await _context.WorkoutRoutines
            .AsNoTracking()
            .Where(r => r.RoutineGroupId == routine.RoutineGroupId)
            .MaxAsync(r => r.Version, cancellationToken);

        if (routine.Version != maxVersionInGroup)
            throw new DomainException("Only the latest version can be published.");

        routine.Publish();

        await _context.SaveChangesAsync(cancellationToken);
    }
}