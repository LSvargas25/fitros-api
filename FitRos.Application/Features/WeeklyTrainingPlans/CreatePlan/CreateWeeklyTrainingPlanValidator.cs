using FluentValidation;

namespace FitRos.Application.Features.WeeklyTrainingPlans.CreatePlan;

public class CreateWeeklyTrainingPlanValidator : AbstractValidator<CreateWeeklyTrainingPlanCommand>
{
    public CreateWeeklyTrainingPlanValidator()
    {
        RuleFor(x => x.ClientProfileId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
    }
}
