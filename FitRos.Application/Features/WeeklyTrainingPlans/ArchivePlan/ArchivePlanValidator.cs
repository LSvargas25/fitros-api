using FluentValidation;

namespace FitRos.Application.Features.WeeklyTrainingPlans.ArchivePlan;

public class ArchivePlanValidator : AbstractValidator<ArchivePlanCommand>
{
    public ArchivePlanValidator()
    {
        RuleFor(x => x.PlanId)
            .NotEmpty();
    }
}
