using FitRos.Application.Abstractions.Persistence;
using FitRos.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Exercises.ArchiveExercise;

public sealed class ArchiveExerciseHandler
    : IRequestHandler<ArchiveExerciseCommand>
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
            throw new DomainException("Exercise not found.");

        exercise.Archive();

        await _context.SaveChangesAsync(cancellationToken);
    }
}