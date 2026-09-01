using MediatR;

namespace FitRos.Application.Features.WorkoutSessions.RemoveSetFromWorkoutSession;

public sealed record RemoveSetFromWorkoutSessionCommand(
    Guid SessionId,
    Guid SetId) : IRequest;
