using FluentValidation;

namespace FitRos.Application.Features.MealPlans.RenameMealPlan;

public class RenameMealPlanValidator : AbstractValidator<RenameMealPlanCommand>
{
    public RenameMealPlanValidator()
    {
        RuleFor(x => x.MealPlanId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
