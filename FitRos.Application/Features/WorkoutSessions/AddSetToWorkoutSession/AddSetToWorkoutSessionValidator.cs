using FluentValidation;

namespace FitRos.Application.Features.WorkoutSessions.AddSetToWorkoutSession;

public sealed class AddSetToWorkoutSessionValidator : AbstractValidator<AddSetToWorkoutSessionCommand>
{
    public AddSetToWorkoutSessionValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.ExerciseId).NotEmpty();
        RuleFor(x => x.SetNumber).GreaterThan(0);
        RuleFor(x => x.RepsAchieved).GreaterThanOrEqualTo(0).LessThanOrEqualTo(200);
        RuleFor(x => x.WeightUsed).GreaterThanOrEqualTo(0);
    }
}
