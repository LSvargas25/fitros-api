using FluentValidation;

namespace FitRos.Application.Features.ClientProfiles.ProgressReport.GetProgressReport;

public sealed class GetProgressReportValidator : AbstractValidator<GetProgressReportQuery>
{
    public GetProgressReportValidator()
    {
        RuleFor(x => x.ClientProfileId).NotEmpty();

        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12)
            .When(x => x.Month.HasValue);

        RuleFor(x => x.Year)
            .InclusiveBetween(2000, 2100)
            .When(x => x.Year.HasValue);

        RuleFor(x => x)
            .Must(x => x.Year.HasValue == x.Month.HasValue)
            .WithMessage("Year and Month must be provided together, or both omitted for the current month.");
    }
}
