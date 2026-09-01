using FluentValidation;

namespace FitRos.Application.Features.MealPlans.ActivateMealPlan;

public class ActivateMealPlanValidator : AbstractValidator<ActivateMealPlanCommand>
{
    public ActivateMealPlanValidator()
    {
        RuleFor(x => x.MealPlanId).NotEmpty();
    }
}
