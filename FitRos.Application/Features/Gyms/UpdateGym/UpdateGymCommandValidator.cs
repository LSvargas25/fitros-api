using FluentValidation;

namespace FitRos.Application.Features.Gyms.UpdateGym;

public sealed class UpdateGymCommandValidator : AbstractValidator<UpdateGymCommand>
{
    public UpdateGymCommandValidator()
    {
        // GymId comes from the route ({gymId:guid}) and is folded in by the
        // controller (command with { GymId = gymId }); it is never in the body,
        // so validating it here would 400 every real request.
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