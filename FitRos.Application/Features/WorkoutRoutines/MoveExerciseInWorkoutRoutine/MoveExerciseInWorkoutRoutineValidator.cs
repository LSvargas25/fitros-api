using FluentValidation;

namespace FitRos.Application.Features.WorkoutRoutines.MoveExerciseInWorkoutRoutine;

public class MoveExerciseInWorkoutRoutineValidator
    : AbstractValidator<MoveExerciseInWorkoutRoutineCommand>
{
    public MoveExerciseInWorkoutRoutineValidator()
    {
        RuleFor(x => x.WorkoutRoutineId)
            .NotEmpty()
            .WithMessage("WorkoutRoutineId is required.");

        RuleFor(x => x.ExerciseId)
            .NotEmpty()
            .WithMessage("ExerciseId is required.");

        RuleFor(x => x.NewOrder)
            .GreaterThan(0)
            .WithMessage("NewOrder must be greater than zero.");
    }
}
