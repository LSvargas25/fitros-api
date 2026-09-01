using MediatR;

namespace FitRos.Application.Features.WorkoutSessions.CompleteWorkoutSession;

public sealed record CompleteWorkoutSessionCommand(Guid SessionId) : IRequest;
