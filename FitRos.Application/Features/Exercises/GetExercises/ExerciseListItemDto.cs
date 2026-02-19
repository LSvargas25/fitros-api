using FitRos.Domain.Entities.Enums;

namespace FitRos.Application.Features.Exercises.GetExercises;

public record ExerciseListItemDto(
    Guid Id,
    string Name,
    MuscleGroup Category
);
