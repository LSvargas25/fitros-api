using FitRos.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Exercises.UpdateExercise;

public class UpdateExerciseHandler
{
    private readonly IFitRosDbContext _context;

    public UpdateExerciseHandler(IFitRosDbContext context)
    {
        _context = context;
    }

    public async Task Handle(
        UpdateExerciseCommand command,
        CancellationToken cancellationToken)
    {
        var exercise = await _context.Exercises
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);

        if (exercise is null)
            throw new KeyNotFoundException("Exercise not found.");

        // Validar nombre duplicado (excluyendo el actual)
        var normalized = command.Name.ToLower();

        var exists = await _context.Exercises
            .AnyAsync(x =>
                x.Id != command.Id &&
                x.NormalizedName == normalized,
                cancellationToken);

        if (exists)
            throw new InvalidOperationException(
                $"Exercise '{command.Name}' already exists.");

        // Delegar al dominio
        exercise.Update(
            command.Name,
            command.Description,
            command.Category
        );

        await _context.SaveChangesAsync(cancellationToken);
    }
}
