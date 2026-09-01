using FitRos.Application.Features.WorkoutSessions;
using MediatR;

namespace FitRos.Application.Features.WorkoutSessions.StartTodaysWorkoutSession;

public sealed record StartTodaysWorkoutSessionCommand(Guid? RoutineId)
    : IRequest<WorkoutSessionDetailsDto>;
