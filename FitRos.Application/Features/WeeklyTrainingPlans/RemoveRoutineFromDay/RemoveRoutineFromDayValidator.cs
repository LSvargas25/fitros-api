using FluentValidation;

namespace FitRos.Application.Features.WeeklyTrainingPlans.RemoveRoutineFromDay;

public class RemoveRoutineFromDayValidator : AbstractValidator<RemoveRoutineFromDayCommand>
{
    public RemoveRoutineFromDayValidator()
    {
        RuleFor(x => x.PlanId)
            .NotEmpty();

        RuleFor(x => x.Day)
            .IsInEnum();
    }
}
