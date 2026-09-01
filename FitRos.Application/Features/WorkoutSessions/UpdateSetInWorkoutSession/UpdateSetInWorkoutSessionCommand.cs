using MediatR;

namespace FitRos.Application.Features.WorkoutSessions.UpdateSetInWorkoutSession;

public sealed record UpdateSetInWorkoutSessionCommand(
    Guid SessionId,
    Guid SetId,
    int SetNumber,
    int RepsAchieved,
    decimal WeightUsed) : IRequest;
