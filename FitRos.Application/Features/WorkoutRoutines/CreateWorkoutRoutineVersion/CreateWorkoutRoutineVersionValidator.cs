using FluentValidation;

namespace FitRos.Application.Features.WorkoutRoutines.CreateWorkoutRoutineVersion;

public sealed class CreateWorkoutRoutineVersionValidator
    : AbstractValidator<CreateWorkoutRoutineVersionCommand>
{
    public CreateWorkoutRoutineVersionValidator()
    {
        RuleFor(x => x.WorkoutRoutineId)
            .NotEmpty()
            .WithMessage("WorkoutRoutineId cannot be empty.");
    }
}