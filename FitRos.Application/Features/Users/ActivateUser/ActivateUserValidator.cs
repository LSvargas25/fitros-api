using FluentValidation;

namespace FitRos.Application.Features.Users.ActivateUser;

public sealed class ActivateUserValidator
    : AbstractValidator<ActivateUserCommand>
{
    public ActivateUserValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}