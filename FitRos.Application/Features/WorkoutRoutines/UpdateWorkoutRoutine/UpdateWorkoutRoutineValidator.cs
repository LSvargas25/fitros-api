using FluentValidation;

namespace FitRos.Application.Features.WorkoutRoutines.UpdateWorkoutRoutine;

public sealed class UpdateWorkoutRoutineValidator
    : AbstractValidator<UpdateWorkoutRoutineCommand>
{
    public UpdateWorkoutRoutineValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Routine name is required.")
            .MinimumLength(3).WithMessage("Routine name must be at least 3 characters.")
            .MaximumLength(100).WithMessage("Routine name must not exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
    }
}
