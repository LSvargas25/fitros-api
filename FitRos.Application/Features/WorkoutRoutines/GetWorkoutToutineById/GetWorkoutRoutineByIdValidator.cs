using FluentValidation;

namespace FitRos.Application.Features.WorkoutRoutines.GetWorkoutRoutineById;

public class GetWorkoutRoutineByIdValidator
    : AbstractValidator<GetWorkoutRoutineByIdQuery>
{
    public GetWorkoutRoutineByIdValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("WorkoutRoutine Id is required.");
    }
}
