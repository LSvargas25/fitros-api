using FluentValidation;

namespace FitRos.Application.Features.WeeklyTrainingPlans.GetPlanById;

public class GetWeeklyTrainingPlanByIdValidator : AbstractValidator<GetWeeklyTrainingPlanByIdQuery>
{
    public GetWeeklyTrainingPlanByIdValidator()
    {
        RuleFor(x => x.PlanId)
            .NotEmpty();
    }
}
