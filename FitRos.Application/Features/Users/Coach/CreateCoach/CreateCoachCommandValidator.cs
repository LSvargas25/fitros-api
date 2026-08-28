using FluentValidation;

namespace FitRos.Application.Features.Users.Coach.CreateCoach;

public sealed class CreateCoachCommandValidator : AbstractValidator<CreateCoachCommand>
{
    public CreateCoachCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}
