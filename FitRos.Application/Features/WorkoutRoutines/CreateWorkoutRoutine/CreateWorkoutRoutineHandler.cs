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
        var normalizedName = command.Name.Trim().ToLowerInvariant();

        var exists = await _context.WorkoutRoutines
            .AnyAsync(r =>
                r.NormalizedName == normalizedName &&
                r.GymId == _currentUser.GymId,
                cancellationToken);

        if (exists)
            throw new DomainException("A routine with this name already exists.");

        var routine = WorkoutRoutine.Create(
            _currentUser.GymId!.Value,
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