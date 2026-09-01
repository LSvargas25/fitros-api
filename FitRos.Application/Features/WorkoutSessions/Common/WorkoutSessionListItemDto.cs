using FitRos.Domain.Enums;

namespace FitRos.Application.Features.WorkoutSessions;

public sealed record WorkoutSessionListItemDto(
    Guid Id,
    DateTime ScheduledDate,
    string RoutineNameSnapshot,
    WorkoutSessionStatus Status,
    int TotalSets);
