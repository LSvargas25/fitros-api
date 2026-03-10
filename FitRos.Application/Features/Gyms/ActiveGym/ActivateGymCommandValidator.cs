using FluentValidation;

namespace FitRos.Application.Features.Gyms.ActivateGym;

public sealed class ActivateGymCommandValidator : AbstractValidator<ActivateGymCommand>
{
    public ActivateGymCommandValidator()
    {
        RuleFor(x => x.GymId)
            .NotEmpty();
    }
}