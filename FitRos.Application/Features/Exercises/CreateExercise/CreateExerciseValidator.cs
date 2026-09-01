using FluentValidation;

namespace FitRos.Application.Features.Exercises.CreateExercise;

public class CreateExerciseValidator
    : AbstractValidator<CreateExerciseCommand>
{
    public CreateExerciseValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty();

        RuleFor(x => x.Category)
            .IsInEnum();
    }
}