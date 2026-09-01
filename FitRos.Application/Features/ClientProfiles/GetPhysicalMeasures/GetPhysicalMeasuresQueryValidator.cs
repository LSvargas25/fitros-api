using FluentValidation;

namespace FitRos.Application.Features.ClientProfiles.GetPhysicalMeasures;

public sealed class GetPhysicalMeasuresQueryValidator : AbstractValidator<GetPhysicalMeasuresQuery>
{
    public GetPhysicalMeasuresQueryValidator()
    {
        RuleFor(x => x.ClientProfileId).NotEmpty();
    }
}
