using FluentValidation;

namespace FitRos.Application.Features.Users.UserManagement.DeactivateUser;

public sealed class DeactivateUserValidator
    : AbstractValidator<DeactivateUserCommand>
{
    public DeactivateUserValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}