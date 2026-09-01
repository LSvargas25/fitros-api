using FluentValidation;

namespace FitRos.Application.Features.Gyms.DeactivateGym;

public sealed class DeactivateGymCommandValidator : AbstractValidator<DeactivateGymCommand>
{
    public DeactivateGymCommandValidator()
    {
        RuleFor(x => x.GymId)
            .NotEmpty();
    }
}