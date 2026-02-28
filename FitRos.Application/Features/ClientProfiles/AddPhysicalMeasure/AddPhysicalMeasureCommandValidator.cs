using FluentValidation;

namespace FitRos.Application.Features.ClientProfiles.AddPhysicalMeasure;

public sealed class AddPhysicalMeasureCommandValidator
    : AbstractValidator<AddPhysicalMeasureCommand>
{
    public AddPhysicalMeasureCommandValidator()
    {
        RuleFor(x => x.ClientProfileId)
            .NotEmpty();

        RuleFor(x => x.Weight)
            .GreaterThan(0)
            .LessThan(500);

        RuleFor(x => x.BodyFatPercentage)
            .InclusiveBetween(0, 100);

        RuleFor(x => x.MuscleMass)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Waist)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Chest)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Arms)
            .GreaterThanOrEqualTo(0);
    }
}