using MediatR;

namespace FitRos.Application.Features.WorkoutSessions.AddSetToWorkoutSession;

public sealed record AddSetToWorkoutSessionCommand(
    Guid SessionId,
    Guid ExerciseId,
    int SetNumber,
    int RepsAchieved,
    decimal WeightUsed) : IRequest;
