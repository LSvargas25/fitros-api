using FitRos.Domain.Enums;

namespace FitRos.Application.Features.WorkoutRoutines.CreateWorkoutRoutineVersion;

public sealed record CreateWorkoutRoutineVersionResponse(
    Guid Id,
    Guid RoutineGroupId,
    int Version,
    RoutineStatus Status
);