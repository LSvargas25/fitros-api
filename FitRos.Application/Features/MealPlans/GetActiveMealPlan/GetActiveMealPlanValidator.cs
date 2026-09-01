using FluentValidation;

namespace FitRos.Application.Features.MealPlans.GetActiveMealPlan;

public class GetActiveMealPlanValidator : AbstractValidator<GetActiveMealPlanQuery>
{
    public GetActiveMealPlanValidator()
    {
        RuleFor(x => x.ClientProfileId).NotEmpty();
    }
}
