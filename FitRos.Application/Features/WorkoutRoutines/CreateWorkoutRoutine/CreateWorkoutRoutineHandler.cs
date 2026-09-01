using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Entities.Training;
using FitRos.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.WorkoutRoutines.CreateWorkoutRoutine;

public class CreateWorkoutRoutineHandler
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public CreateWorkoutRoutineHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<CreateWorkoutRoutineResponse> Handle(
        CreateWorkoutRoutineCommand command,
        CancellationToken cancellationToken)
    {
        // Guard the nullable: OwnerApp (and any gym-less caller) has no GymId, and
        // WorkoutRoutine.Create needs a concrete gym. Previously `GymId!.Value`
        // threw a raw "Nullable object must have a value." (surfaced as a 400 with
        // that message). Authentication itself is enforced by [Authorize] on the
        // controller, since these actions don't run through the MediatR pipeline.
        var gymId = _currentUser.GymId
            ?? throw new DomainException("You must be assigned to a gym to create a workout routine.");

        var normalizedName = command.Name.Trim().ToLowerInvariant();

        var exists = await _context.WorkoutRoutines
            .AnyAsync(r =>
                r.NormalizedName == normalizedName &&
                r.GymId == gymId,
                cancellationToken);

        if (exists)
            throw new DomainException("A routine with this name already exists.");

        var routine = WorkoutRoutine.Create(
            gymId,
            command.Name.Trim(),
            command.Description
        );

        _context.WorkoutRoutines.Add(routine);

        await _context.SaveChangesAsync(cancellationToken);

        return new CreateWorkoutRoutineResponse(
            routine.Id,
            routine.Name,
            routine.Version
        );
    }
}