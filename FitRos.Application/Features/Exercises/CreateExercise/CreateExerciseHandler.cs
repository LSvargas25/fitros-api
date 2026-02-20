using FitRos.Application.Abstractions.Persistence;
using FitRos.Domain.Entities.Training;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Exercises.CreateExercise;

public class CreateExerciseHandler
{
    private readonly IFitRosDbContext _context;

    public CreateExerciseHandler(IFitRosDbContext context)
    {
        _context = context;
    }

    public async Task<CreateExerciseResponse> Handle(
     CreateExerciseCommand command,
     CancellationToken cancellationToken)
    {
        var normalized = command.Name.ToLower();

        var exists = await _context.Exercises
            .AnyAsync(x => x.NormalizedName == normalized, cancellationToken);

        if (exists)
            throw new InvalidOperationException(
                $"Exercise '{command.Name}' already exists.");

        var exercise = Exercise.Create(
            command.Name,
            command.Description,
            command.Category
        );

        _context.Exercises.Add(exercise);

        await _context.SaveChangesAsync(cancellationToken);

        return new CreateExerciseResponse(exercise.Id);
    }

}
