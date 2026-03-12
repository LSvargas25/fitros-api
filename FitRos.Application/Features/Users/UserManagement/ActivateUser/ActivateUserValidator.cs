using FluentValidation;

namespace FitRos.Application.Features.Users.UserManagement.ActivateUser;

public sealed class ActivateUserValidator
    : AbstractValidator<ActivateUserCommand>
{
    public ActivateUserValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}