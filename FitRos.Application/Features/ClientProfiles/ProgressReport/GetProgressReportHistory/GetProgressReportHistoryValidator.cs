using FluentValidation;

namespace FitRos.Application.Features.ClientProfiles.ProgressReport.GetProgressReportHistory;

public sealed class GetProgressReportHistoryValidator
    : AbstractValidator<GetProgressReportHistoryQuery>
{
    public GetProgressReportHistoryValidator()
    {
        RuleFor(x => x.ClientProfileId).NotEmpty();
    }
}
