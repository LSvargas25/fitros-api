using FitRos.Domain.Entities.Enums;
using MediatR;

namespace FitRos.Application.Features.Exercises.CreateExercise;

public record CreateExerciseCommand(
    string Name,
    string Description,
    MuscleGroup Category
) : IRequest<CreateExerciseResponse>;