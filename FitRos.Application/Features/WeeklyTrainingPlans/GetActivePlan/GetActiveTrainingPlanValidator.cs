using FluentValidation;

namespace FitRos.Application.Features.WeeklyTrainingPlans.GetActivePlan;

public class GetActiveTrainingPlanValidator : AbstractValidator<GetActiveTrainingPlanQuery>
{
    public GetActiveTrainingPlanValidator()
    {
        RuleFor(x => x.ClientProfileId)
            .NotEmpty();
    }
}
