using FitRos.Application.Features.WorkoutSessions;
using MediatR;

namespace FitRos.Application.Features.WorkoutSessions.GetMyWorkoutSessionHistory;

public sealed record GetMyWorkoutSessionHistoryQuery(
    DateTime? From,
    DateTime? To) : IRequest<List<WorkoutSessionListItemDto>>;
