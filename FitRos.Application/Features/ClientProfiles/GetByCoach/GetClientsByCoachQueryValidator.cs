using FluentValidation;

namespace FitRos.Application.Features.ClientProfiles.GetByCoach
{
    public sealed class GetClientsByCoachQueryValidator
        : AbstractValidator<GetClientsByCoachQuery>
    {
        public GetClientsByCoachQueryValidator()
        {
            RuleFor(x => x.CoachId)
                .NotEmpty();
        }
    }
}