using FluentValidation;

namespace FitRos.Application.Features.WorkoutRoutines.RemoveExerciseFromWorkoutRoutine;

public class RemoveExerciseFromWorkoutRoutineValidator
    : AbstractValidator<RemoveExerciseFromWorkoutRoutineCommand>
{
    public RemoveExerciseFromWorkoutRoutineValidator()
    {
        RuleFor(x => x.WorkoutRoutineId)
            .NotEmpty()
            .WithMessage("WorkoutRoutineId is required.");

        RuleFor(x => x.ExerciseId)
            .NotEmpty()
            .WithMessage("ExerciseId is required.");
    }
}
