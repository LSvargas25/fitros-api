using FluentValidation;

namespace FitRos.Application.Features.ClientProfiles.ReassignCoach
{
    public sealed class ReassignClientCoachCommandValidator
        : AbstractValidator<ReassignClientCoachCommand>
    {
        public ReassignClientCoachCommandValidator()
        {
            RuleFor(x => x.ClientId).NotEmpty();
            RuleFor(x => x.NewCoachId).NotEmpty();
        }
    }
}