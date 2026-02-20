using FitRos.Application.Abstractions.Persistence;
using FitRos.Domain.Common;
using FitRos.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.WorkoutRoutines.CreateWorkoutRoutineVersion;

public sealed class CreateWorkoutRoutineVersionHandler
{
    private readonly IFitRosDbContext _context;

    public CreateWorkoutRoutineVersionHandler(IFitRosDbContext context)
    {
        _context = context;
    }

    public async Task<CreateWorkoutRoutineVersionResponse> Handle(
        CreateWorkoutRoutineVersionCommand command,
        CancellationToken cancellationToken)
    {
        // 1️⃣ Load routine
        var routine = await _context.WorkoutRoutines
            .Include(r => r.Exercises)
            .FirstOrDefaultAsync(
                r => r.Id == command.WorkoutRoutineId,
                cancellationToken);

        if (routine is null)
            throw new KeyNotFoundException("Workout routine not found.");

        // 2️⃣ Must be Published to create a new version
        if (routine.Status != RoutineStatus.Published)
            throw new DomainException("Only published routines can be versioned.");

        // 3️⃣ Only one Draft per RoutineGroupId
        var draftExists = await _context.WorkoutRoutines
            .AsNoTracking()
            .AnyAsync(r =>
                r.RoutineGroupId == routine.RoutineGroupId &&
                r.Status == RoutineStatus.Draft,
                cancellationToken);

        if (draftExists)
            throw new DomainException("A draft version already exists for this routine group.");

        // 4️⃣ Create new version from aggregate
        var newRoutine = routine.CreateNewVersion();

        // 5️⃣ Persist
        _context.WorkoutRoutines.Add(newRoutine);
        await _context.SaveChangesAsync(cancellationToken);

        return new CreateWorkoutRoutineVersionResponse(
            newRoutine.Id,
            newRoutine.RoutineGroupId,
            newRoutine.Version,
            newRoutine.Status
        );
    }
}