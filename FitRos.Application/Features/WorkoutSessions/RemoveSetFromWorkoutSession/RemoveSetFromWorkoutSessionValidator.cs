using FluentValidation;

namespace FitRos.Application.Features.WorkoutSessions.RemoveSetFromWorkoutSession;

public sealed class RemoveSetFromWorkoutSessionValidator : AbstractValidator<RemoveSetFromWorkoutSessionCommand>
{
    public RemoveSetFromWorkoutSessionValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.SetId).NotEmpty();
    }
}
