using FitRos.Application.Abstractions.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.Exercises.GetExercises;

public sealed class GetExercisesHandler
    : IRequestHandler<GetExercisesQuery, List<ExerciseListItemDto>>
{
    private readonly IFitRosDbContext _context;

    public GetExercisesHandler(IFitRosDbContext context)
    {
        _context = context;
    }

    public async Task<List<ExerciseListItemDto>> Handle(
        GetExercisesQuery query,
        CancellationToken cancellationToken)
    {
        var exercises = _context.Exercises.AsQueryable();

        if (query.Category.HasValue)
            exercises = exercises.Where(x => x.Category == query.Category.Value);

        return await exercises
            .Select(x => new ExerciseListItemDto(
                x.Id,
                x.Name,
                x.Category))
            .ToListAsync(cancellationToken);
    }
}