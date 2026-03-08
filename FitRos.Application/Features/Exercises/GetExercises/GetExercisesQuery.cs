using FitRos.Domain.Entities.Enums;
using MediatR;

namespace FitRos.Application.Features.Exercises.GetExercises;

public record GetExercisesQuery(
    MuscleGroup? Category
) : IRequest<List<ExerciseListItemDto>>;