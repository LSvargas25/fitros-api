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

        // Optional back-dating window: not in the future, and no more than
        // 60 days back (forgot-to-log recovery, not history rewriting).
        When(x => x.RecordedAt.HasValue, () =>
        {
            RuleFor(x => x.RecordedAt!.Value)
                .LessThanOrEqualTo(_ => DateTime.UtcNow.AddMinutes(1))
                .WithMessage("RecordedAt cannot be in the future.")
                .GreaterThanOrEqualTo(_ => DateTime.UtcNow.AddDays(-60))
                .WithMessage("RecordedAt cannot be more than 60 days in the past.");
        });
    }
}