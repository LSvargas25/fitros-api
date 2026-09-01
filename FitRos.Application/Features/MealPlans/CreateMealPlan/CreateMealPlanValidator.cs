using FluentValidation;

namespace FitRos.Application.Features.MealPlans.CreateMealPlan;

public class CreateMealPlanValidator : AbstractValidator<CreateMealPlanCommand>
{
    public CreateMealPlanValidator()
    {
        RuleFor(x => x.ClientProfileId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
    }
}
