using FitRos.Application.Abstractions.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Exercises.GetExerciseById;

public sealed class GetExerciseByIdHandler
    : IRequestHandler<GetExerciseByIdQuery, ExerciseDto?>
{
    private readonly IFitRosDbContext _context;

    public GetExerciseByIdHandler(IFitRosDbContext context)
    {
        _context = context;
    }

    public async Task<ExerciseDto?> Handle(
        GetExerciseByIdQuery query,
        CancellationToken cancellationToken)
    {
        return await _context.Exercises
            .Where(x => x.Id == query.Id)
            .Select(x => new ExerciseDto(
                x.Id,
                x.Name,
                x.Description,
                x.Category))
            .FirstOrDefaultAsync(cancellationToken);
    }
}