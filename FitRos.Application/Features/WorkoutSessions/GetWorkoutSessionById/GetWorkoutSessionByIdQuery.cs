using FitRos.Application.Features.WorkoutSessions;
using MediatR;

namespace FitRos.Application.Features.WorkoutSessions.GetWorkoutSessionById;

public sealed record GetWorkoutSessionByIdQuery(Guid SessionId) : IRequest<WorkoutSessionDetailsDto?>;
