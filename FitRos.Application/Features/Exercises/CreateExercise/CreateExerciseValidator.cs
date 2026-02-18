using FluentValidation;

namespace FitRos.Application.Features.Exercises.CreateExercise;

public class CreateExerciseValidator
    : AbstractValidator<CreateExerciseCommand>
{
    public CreateExerciseValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Exercise name is required.");

        RuleFor(x => x.Category)
            .IsInEnum()
            .WithMessage("Invalid muscle group category.");
    }
}
