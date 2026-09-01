using FluentValidation;

namespace FitRos.Application.Features.MealPlans.RemoveMealPlanEntry;

public class RemoveMealPlanEntryValidator : AbstractValidator<RemoveMealPlanEntryCommand>
{
    public RemoveMealPlanEntryValidator()
    {
        RuleFor(x => x.MealPlanId).NotEmpty();
        RuleFor(x => x.EntryId).NotEmpty();
    }
}
