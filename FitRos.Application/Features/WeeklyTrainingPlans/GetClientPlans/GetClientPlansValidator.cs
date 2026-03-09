using FluentValidation;

namespace FitRos.Application.Features.WeeklyTrainingPlans.GetClientPlans;

public class GetClientPlansValidator : AbstractValidator<GetClientPlansQuery>
{
    public GetClientPlansValidator()
    {
        RuleFor(x => x.ClientProfileId)
            .NotEmpty();
    }
}
