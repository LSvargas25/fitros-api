using FitRos.Domain.Entities.Enums;

namespace FitRos.Application.Features.Exercises.GetExerciseById;

public record ExerciseDto(
    Guid Id,
    string Name,
    string Description,
    MuscleGroup Category);
