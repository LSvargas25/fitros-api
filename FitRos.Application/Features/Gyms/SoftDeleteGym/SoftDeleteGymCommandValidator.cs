using FluentValidation;

namespace FitRos.Application.Features.Gyms.SoftDeleteGym;

public sealed class SoftDeleteGymCommandValidator : AbstractValidator<SoftDeleteGymCommand>
{
    public SoftDeleteGymCommandValidator()
    {
        RuleFor(x => x.GymId)
            .NotEmpty();
    }
}