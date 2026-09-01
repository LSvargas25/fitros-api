using FluentValidation;

namespace FitRos.Application.Features.WorkoutRoutines.ArchiveWorkoutRoutine;

public class ArchiveWorkoutRoutineValidator
    : AbstractValidator<ArchiveWorkoutRoutineCommand>
{
    public ArchiveWorkoutRoutineValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Workout routine id is required.");
    }
}
