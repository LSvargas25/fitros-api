using FitRos.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using FitRos.Domain.Common;

namespace FitRos.Application.Features.Exercises.ArchiveExercise;

public class ArchiveExerciseHandler
{
    private readonly IFitRosDbContext _context;

    public ArchiveExerciseHandler(IFitRosDbContext context)
    {
        _context = context;
    }

    public async Task Handle(
        ArchiveExerciseCommand command,
        CancellationToken cancellationToken)
    {
        var exercise = await _context.Exercises
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);

        if (exercise is null)
            throw new NotFoundException("Exercise not found.");

        // Delegamos al dominio
        exercise.Archive();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
