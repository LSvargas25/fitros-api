using FluentValidation;

namespace FitRos.Application.Features.WorkoutRoutines.UpdateExerciseInWorkoutRoutine;

public class UpdateExerciseInWorkoutRoutineValidator
    : AbstractValidator<UpdateExerciseInWorkoutRoutineCommand>
{
    public UpdateExerciseInWorkoutRoutineValidator()
    {
        RuleFor(x => x.WorkoutRoutineId)
            .NotEmpty()
            .WithMessage("WorkoutRoutineId is required.");

        RuleFor(x => x.ExerciseId)
            .NotEmpty()
            .WithMessage("ExerciseId is required.");

        RuleFor(x => x.SuggestedSets)
            .GreaterThan(0)
            .WithMessage("SuggestedSets must be greater than 0.");

        RuleFor(x => x.SuggestedReps)
            .GreaterThan(0)
            .WithMessage("SuggestedReps must be greater than 0.");

        RuleFor(x => x.SuggestedRestSeconds)
            .GreaterThanOrEqualTo(0)
            .WithMessage("SuggestedRestSeconds cannot be negative.");
    }
}
