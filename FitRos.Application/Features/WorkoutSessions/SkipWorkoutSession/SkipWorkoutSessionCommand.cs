using MediatR;

namespace FitRos.Application.Features.WorkoutSessions.SkipWorkoutSession;

public sealed record SkipWorkoutSessionCommand(Guid SessionId) : IRequest;
