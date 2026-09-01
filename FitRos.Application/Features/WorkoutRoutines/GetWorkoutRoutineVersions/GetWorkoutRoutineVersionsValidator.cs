using FluentValidation;

namespace FitRos.Application.Features.WorkoutRoutines.GetWorkoutRoutineVersions;

public sealed class GetWorkoutRoutineVersionsValidator
    : AbstractValidator<GetWorkoutRoutineVersionsQuery>
{
    public GetWorkoutRoutineVersionsValidator()
    {
        RuleFor(x => x.WorkoutRoutineId)
            .NotEmpty()
            .WithMessage("WorkoutRoutineId cannot be empty.");
    }
}