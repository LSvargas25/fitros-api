using FluentValidation;

namespace FitRos.Application.Features.WeeklyTrainingPlans.AssignRoutineToDay;

public class AssignRoutineToDayValidator : AbstractValidator<AssignRoutineToDayCommand>
{
    public AssignRoutineToDayValidator()
    {
        RuleFor(x => x.PlanId)
            .NotEmpty();

        RuleFor(x => x.WorkoutRoutineId)
            .NotEmpty();

        RuleFor(x => x.Day)
            .IsInEnum();
    }
}
