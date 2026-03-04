using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Training;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Exercises.CreateExercise;

public class CreateExerciseHandler
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public CreateExerciseHandler(
        IFitRosDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<CreateExerciseResponse> Handle(
        CreateExerciseCommand command,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedException("User not authenticated.");

        var normalizedName = command.Name.ToLower();

        var exists = await _context.Exercises
            .AnyAsync(x => x.NormalizedName == normalizedName, cancellationToken);

        if (exists)
            throw new DomainException("Exercise already exists.");

        var exercise = Exercise.Create(
            command.Name,
            command.Description,
            command.Category,
            _currentUser.GymId);

        _context.Exercises.Add(exercise);
        await _context.SaveChangesAsync(cancellationToken);

        return new CreateExerciseResponse(exercise.Id);
    }
}