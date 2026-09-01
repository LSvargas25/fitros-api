using FluentValidation;

namespace FitRos.Application.Features.ClientProfiles.GetKpiSnapshots;

public sealed class GetClientKpiSnapshotsValidator : AbstractValidator<GetClientKpiSnapshotsQuery>
{
    public GetClientKpiSnapshotsValidator()
    {
        RuleFor(x => x.ClientProfileId).NotEmpty();
    }
}
