using FitRos.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace FitRos.Application.Features.Exercises.GetExerciseById;

public class GetExerciseByIdHandler
{
    private readonly IFitRosDbContext _context;

    public GetExerciseByIdHandler(IFitRosDbContext context)
    {
        _context = context;
    }

    public async Task<ExerciseDto?> Handle(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await _context.Exercises
            .Where(x => x.Id == id)
            .Select(x => new ExerciseDto(
                x.Id,
                x.Name,
                x.Description,
                x.Category))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
