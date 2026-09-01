using FitRos.Domain.Entities.Enums;
using MediatR;

namespace FitRos.Application.Features.Exercises.UpdateExercise;

public record UpdateExerciseCommand(
    Guid Id,
    string Name,
    string Description,
    MuscleGroup Category
) : IRequest;