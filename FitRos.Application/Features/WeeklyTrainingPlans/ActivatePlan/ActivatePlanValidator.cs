using FluentValidation;

namespace FitRos.Application.Features.WeeklyTrainingPlans.ActivatePlan;

public class ActivatePlanValidator : AbstractValidator<ActivatePlanCommand>
{
    public ActivatePlanValidator()
    {
        RuleFor(x => x.PlanId)
            .NotEmpty();
    }
}
