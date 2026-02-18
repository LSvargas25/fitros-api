using FluentValidation;
using FitRos.Domain.Enums;

namespace FitRos.Application.Features.WorkoutRoutines.GetWorkoutRoutines;

public class GetWorkoutRoutinesValidator
    : AbstractValidator<GetWorkoutRoutinesQuery>
{
    public GetWorkoutRoutinesValidator()
    {
        RuleFor(x => x.Status)
            .Must(BeAValidStatus)
            .When(x => x.Status.HasValue)
            .WithMessage("Invalid routine status value.");
    }

    private bool BeAValidStatus(RoutineStatus? status)
    {
        if (!status.HasValue)
            return true;

        return Enum.IsDefined(typeof(RoutineStatus), status.Value);
    }
}
