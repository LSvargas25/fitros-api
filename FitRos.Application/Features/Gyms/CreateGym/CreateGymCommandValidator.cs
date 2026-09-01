using FluentValidation;

namespace FitRos.Application.Features.Gyms.CreateGym;

public sealed class CreateGymCommandValidator : AbstractValidator<CreateGymCommand>
{
    public CreateGymCommandValidator()
    {
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