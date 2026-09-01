namespace FitRos.Application.Features.ClientProfiles.GetKpiSnapshots;

public sealed record ClientKpiSnapshotDto(
    Guid Id,
    Guid PhysicalMeasureId,
    decimal? WeightDelta,
    decimal? BodyFatDelta,
    decimal? WaistDelta,
    DateTime CreatedAtUtc);
