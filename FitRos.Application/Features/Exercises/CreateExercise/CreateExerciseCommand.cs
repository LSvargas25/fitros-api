using FitRos.Domain.Entities.Enums; 

namespace FitRos.Application.Features.Exercises.CreateExercise;

public record CreateExerciseCommand(
    string Name,
    string Description,
    MuscleGroup Category
);
