using FluentValidation;

namespace FitRos.Application.Features.Exercises.UpdateExercise;

public class UpdateExerciseValidator
    : AbstractValidator<UpdateExerciseCommand>
{
    public UpdateExerciseValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Exercise name is required.");

        RuleFor(x => x.Category)
            .IsInEnum()
            .WithMessage("Invalid muscle group category.");
    }
}
