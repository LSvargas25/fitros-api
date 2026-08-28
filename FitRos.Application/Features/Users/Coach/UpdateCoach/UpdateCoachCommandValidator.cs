using FluentValidation;

namespace FitRos.Application.Features.Users.Coach.UpdateCoach;

public sealed class UpdateCoachCommandValidator : AbstractValidator<UpdateCoachCommand>
{
    public UpdateCoachCommandValidator()
    {
        // CoachId is taken from the route ({id:guid}) and folded in by the
        // controller (command with { CoachId = id }); it is never supplied in
        // the body, so validating it here would 400 every real request.
        RuleFor(x => x.Email).EmailAddress().MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.FirstName).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.FirstName));
        RuleFor(x => x.LastName).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.LastName));
    }
}
