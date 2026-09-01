using FitRos.Domain.Enums;

namespace FitRos.Application.Features.WorkoutRoutines.GetWorkoutRoutineVersions;

public sealed record WorkoutRoutineVersionListItem(
    Guid Id,
    Guid RoutineGroupId,
    int Version,
    RoutineStatus Status,
    DateTime CreatedAt
);