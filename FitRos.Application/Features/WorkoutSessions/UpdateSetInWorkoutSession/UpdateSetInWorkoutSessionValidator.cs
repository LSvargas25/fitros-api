using FluentValidation;

namespace FitRos.Application.Features.WorkoutSessions.UpdateSetInWorkoutSession;

public sealed class UpdateSetInWorkoutSessionValidator : AbstractValidator<UpdateSetInWorkoutSessionCommand>
{
    public UpdateSetInWorkoutSessionValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.SetId).NotEmpty();
        RuleFor(x => x.SetNumber).GreaterThan(0);
        RuleFor(x => x.RepsAchieved).GreaterThanOrEqualTo(0).LessThanOrEqualTo(200);
        RuleFor(x => x.WeightUsed).GreaterThanOrEqualTo(0);
    }
}
