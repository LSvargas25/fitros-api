using MediatR;

namespace FitRos.Application.Features.Exercises.GetExerciseById;

public record GetExerciseByIdQuery(Guid Id) : IRequest<ExerciseDto?>;