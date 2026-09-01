using FluentValidation;

namespace FitRos.Application.Features.MealPlans.UpdateMealPlanEntry;

public class UpdateMealPlanEntryValidator : AbstractValidator<UpdateMealPlanEntryCommand>
{
    public UpdateMealPlanEntryValidator()
    {
        RuleFor(x => x.MealPlanId).NotEmpty();
        RuleFor(x => x.EntryId).NotEmpty();
        RuleFor(x => x.QuantityGrams).GreaterThan(0);
    }
}
