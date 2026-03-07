using FitRos.Application.Abstractions.Persistence;
using FitRos.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Exercises.UpdateExercise;

public sealed class UpdateExerciseHandler
    : IRequestHandler<UpdateExerciseCommand>
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
            throw new DomainException("Exercise not found.");

        exercise.Update(
            command.Name,
            command.Description,
            command.Category);

        await _context.SaveChangesAsync(cancellationToken);
    }
}