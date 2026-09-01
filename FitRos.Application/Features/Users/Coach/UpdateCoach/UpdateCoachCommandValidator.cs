using FluentValidation;

namespace FitRos.Application.Features.Users.Coach.UpdateCoach;

public sealed class UpdateCoachCommandValidator : AbstractValidator<UpdateCoachCommand>
{
    public UpdateCoachCommandValidator()
    {
        RuleFor(x => x.CoachId).NotEmpty();
        RuleFor(x => x.Email).EmailAddress().MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.FirstName).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.FirstName));
        RuleFor(x => x.LastName).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.LastName));
    }
}
