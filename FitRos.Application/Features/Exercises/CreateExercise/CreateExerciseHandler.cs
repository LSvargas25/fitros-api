using FitRos.Application.Abstractions.Persistence;
using FitRos.Domain.Entities.Training;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace FitRos.Application.Features.Exercises.CreateExercise;

internal class CreateExerciseHandler
{
    private readonly IFitRosDbContext _context;

    public CreateExerciseHandler(IFitRosDbContext context)
    {
        _context = context;
    }

    public async Task Handle(
        CreateExerciseCommand command,
        CancellationToken cancellationToken)
    {
        // 1️⃣ Validar que no exista un ejercicio con el mismo nombre
        var exists = await _context.Exercises
            .AnyAsync(x => x.Name == command.Name, cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException(
                $"An exercise with the name '{command.Name}' already exists.");
        }

        //  create entity exercise
        var exercise = Exercise.Create(
            command.Name,
            command.Description,
            command.Category
        );

        //Add exercise to database
        _context.AddExercise(exercise);

        // save changes to database
        await _context.SaveChangesAsync(cancellationToken);
    }
}
