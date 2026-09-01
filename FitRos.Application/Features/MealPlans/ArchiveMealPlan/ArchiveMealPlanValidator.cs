using FluentValidation;

namespace FitRos.Application.Features.MealPlans.ArchiveMealPlan;

public class ArchiveMealPlanValidator : AbstractValidator<ArchiveMealPlanCommand>
{
    public ArchiveMealPlanValidator()
    {
        RuleFor(x => x.MealPlanId).NotEmpty();
    }
}
