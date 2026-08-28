using FluentValidation;

namespace FitRos.Application.Features.Users.Admin.UpdateAdmin;

public sealed class UpdateAdminCommandValidator : AbstractValidator<UpdateAdminCommand>
{
    public UpdateAdminCommandValidator()
    {
        // AdminId comes from the route and is folded in by the controller
        // (command with { AdminId = id }); validating it here 400s a valid body.
        RuleFor(x => x.Email).EmailAddress().MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.FirstName).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.FirstName));
        RuleFor(x => x.LastName).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.LastName));
    }
}
