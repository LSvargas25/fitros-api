using FluentValidation;

namespace FitRos.Application.Features.MealPlans.GetClientMealPlans;

public class GetClientMealPlansValidator : AbstractValidator<GetClientMealPlansQuery>
{
    public GetClientMealPlansValidator()
    {
        RuleFor(x => x.ClientProfileId).NotEmpty();
    }
}
