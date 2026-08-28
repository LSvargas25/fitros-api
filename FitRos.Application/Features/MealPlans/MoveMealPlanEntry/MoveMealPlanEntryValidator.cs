using FluentValidation;

namespace FitRos.Application.Features.MealPlans.MoveMealPlanEntry;

public sealed class MoveMealPlanEntryValidator : AbstractValidator<MoveMealPlanEntryCommand>
{
    public MoveMealPlanEntryValidator()
    {
        RuleFor(x => x.MealPlanId).NotEmpty();
        RuleFor(x => x.EntryId).NotEmpty();
        RuleFor(x => x.NewOrder)
            .GreaterThan(0)
            .WithMessage("New order must be at least 1.");
    }
}
