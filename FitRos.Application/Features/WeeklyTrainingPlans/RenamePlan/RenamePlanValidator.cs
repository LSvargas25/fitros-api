using FluentValidation;

namespace FitRos.Application.Features.WeeklyTrainingPlans.RenamePlan;

public class RenamePlanValidator : AbstractValidator<RenamePlanCommand>
{
    public RenamePlanValidator()
    {
        RuleFor(x => x.PlanId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
    }
}
