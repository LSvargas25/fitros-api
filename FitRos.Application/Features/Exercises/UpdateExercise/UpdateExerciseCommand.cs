using FitRos.Domain.Entities.Enums;

namespace FitRos.Application.Features.Exercises.UpdateExercise;

public record UpdateExerciseCommand(
    Guid Id,
    string Name,
    string Description,
    MuscleGroup Category
);
