using FluentValidation;

namespace FitRos.Application.Features.ClientProfiles.GenerateKpiSnapshot;

public sealed class GenerateClientKpiSnapshotCommandValidator
    : AbstractValidator<GenerateClientKpiSnapshotCommand>
{
    public GenerateClientKpiSnapshotCommandValidator()
    {
        RuleFor(x => x.ClientProfileId).NotEmpty();
    }
}
