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

        RuleFor(x => x)
            .Must(x => x.ExistingAdminUserId.HasValue ||
                       (!string.IsNullOrWhiteSpace(x.AdminEmail)
                       && !string.IsNullOrWhiteSpace(x.AdminPassword)))
            .WithMessage("Either ExistingAdminUserId or Admin credentials must be provided.");
    }
}