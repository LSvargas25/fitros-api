using FluentValidation;

namespace FitRos.Application.Features.MealPlans.AddMealPlanEntry;

public class AddMealPlanEntryValidator : AbstractValidator<AddMealPlanEntryCommand>
{
    public AddMealPlanEntryValidator()
    {
        RuleFor(x => x.MealPlanId).NotEmpty();
        RuleFor(x => x.FoodId).NotEmpty();
        RuleFor(x => x.Day).IsInEnum();
        RuleFor(x => x.Meal).IsInEnum();
        RuleFor(x => x.QuantityGrams).GreaterThan(0);
    }
}
