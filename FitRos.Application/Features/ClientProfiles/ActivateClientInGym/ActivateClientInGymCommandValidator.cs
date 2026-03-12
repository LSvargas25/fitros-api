using FluentValidation;

namespace FitRos.Application.Features.ClientProfiles.ActivateClientInGym;

public sealed class ActivateClientInGymCommandValidator : AbstractValidator<ActivateClientInGymCommand>
{
    public ActivateClientInGymCommandValidator()
    {
        RuleFor(x => x.ClientProfileId).NotEmpty();
        RuleFor(x => x.GymId).NotEmpty();
    }
}
