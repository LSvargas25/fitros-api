using FluentValidation;

namespace FitRos.Application.Features.Gyms.UpdateGym;

public sealed class UpdateGymCommandValidator : AbstractValidator<UpdateGymCommand>
{
    public UpdateGymCommandValidator()
    {
        RuleFor(x => x.GymId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Address)
            .NotEmpty()
            .MaximumLength(300);

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .MaximumLength(50);
    }
}