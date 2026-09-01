using FluentValidation;

namespace FitRos.Application.Features.MealPlans.GetMealPlanById;

public class GetMealPlanByIdValidator : AbstractValidator<GetMealPlanByIdQuery>
{
    public GetMealPlanByIdValidator()
    {
        RuleFor(x => x.MealPlanId).NotEmpty();
    }
}
