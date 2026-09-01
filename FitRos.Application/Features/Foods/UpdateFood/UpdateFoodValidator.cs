using FluentValidation;

namespace FitRos.Application.Features.Foods.UpdateFood;

public class UpdateFoodValidator : AbstractValidator<UpdateFoodCommand>
{
    public UpdateFoodValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Category)
            .IsInEnum();

        RuleFor(x => x.CaloriesPer100g).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ProteinPer100g).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CarbsPer100g).GreaterThanOrEqualTo(0);
        RuleFor(x => x.FatPer100g).GreaterThanOrEqualTo(0);

        RuleFor(x => x.ServingSizeGrams)
            .GreaterThan(0)
            .When(x => x.ServingSizeGrams.HasValue);
    }
}
